using System.Text.Json;
using DoctorRegistry.Application.Imports;
using RabbitMQ.Client;

namespace DoctorRegistry.Infrastructure.Messaging;

public sealed class RabbitMqPractitionerImportPublisher(
    RabbitMqConnection connection,
    RabbitMqTopology topology,
    RabbitMqOptions options) : IPractitionerImportPublisher
{
    public async Task PublishAsync(PractitionerImportMessageV1 message, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(options.RetryPublishConfirmTimeoutSeconds));

        await topology.EnsureAsync(timeout.Token);

        await using var channel = await (await connection.GetAsync(timeout.Token)).CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            timeout.Token);

        channel.BasicReturnAsync += (_, args) =>
            Task.FromException(new InvalidOperationException($"Unroutable practitioner import: {args.ReplyText}"));

        var properties = new BasicProperties
        {
            MessageId = message.MessageId.ToString(),
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?> { ["schema-version"] = message.SchemaVersion }
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await channel.BasicPublishAsync(RabbitMqTopology.ImportExchange, RabbitMqTopology.ImportRoutingKey,
            mandatory: true, properties, body, timeout.Token);
    }
}
