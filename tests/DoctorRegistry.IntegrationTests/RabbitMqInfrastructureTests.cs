using DoctorRegistry.Infrastructure.Messaging;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace DoctorRegistry.IntegrationTests;

public sealed class RabbitMqInfrastructureTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder().WithImage("rabbitmq:4-alpine").Build();
    public Task InitializeAsync() => _rabbit.StartAsync();
    public Task DisposeAsync() => _rabbit.DisposeAsync().AsTask();

    [Fact]
    public async Task Topology_declaration_is_repeatable_and_routes_to_main_queue()
    {
        var uri = new Uri(_rabbit.GetConnectionString());
        var credentials = uri.UserInfo.Split(':', 2);
        var options = new RabbitMqOptions
        {
            Host = uri.Host,
            Port = uri.Port,
            UserName = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            VirtualHost = string.IsNullOrEmpty(uri.AbsolutePath.Trim('/'))
                ? "/"
                : Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'))
        };
        await using var connection = new RabbitMqConnection(options);
        var topology = new RabbitMqTopology(connection);
        await topology.EnsureAsync();
        await topology.EnsureAsync();

        await using var channel = await (await connection.GetAsync(default)).CreateChannelAsync();
        await channel.BasicPublishAsync(RabbitMqTopology.ImportExchange, RabbitMqTopology.ImportRoutingKey, true,
            new BasicProperties { DeliveryMode = DeliveryModes.Persistent }, "{}"u8.ToArray());
        var delivery = await channel.BasicGetAsync(RabbitMqTopology.ImportQueue, autoAck: true);
        Assert.NotNull(delivery);
    }
}