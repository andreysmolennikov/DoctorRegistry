using RabbitMQ.Client;

namespace DoctorRegistry.Infrastructure.Messaging;

public sealed class RabbitMqTopology(RabbitMqConnection connection)
{
    public const string ImportExchange = "doctor-registry.practitioner-import";
    public const string RetryExchange = "doctor-registry.practitioner-import.retry";
    public const string DeadLetterExchange = "doctor-registry.practitioner-import.dlx";
    public const string ImportQueue = "doctor-registry.practitioner-import";
    public const string Retry30SecondsQueue = "doctor-registry.practitioner-import.retry.30s";
    public const string Retry5MinutesQueue = "doctor-registry.practitioner-import.retry.5m";
    public const string Retry30MinutesQueue = "doctor-registry.practitioner-import.retry.30m";
    public const string DeadLetterQueue = "doctor-registry.practitioner-import.dlq";
    public const string ImportRoutingKey = "practitioner.import.v1";

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        await using var channel =
            await (await connection.GetAsync(cancellationToken)).CreateChannelAsync(
                cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(ImportExchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(ImportQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(ImportQueue, ImportExchange, ImportRoutingKey,
            cancellationToken: cancellationToken);
        await DeclareRetryAsync(channel, Retry30SecondsQueue, 30_000, cancellationToken);
        await DeclareRetryAsync(channel, Retry5MinutesQueue, 300_000, cancellationToken);
        await DeclareRetryAsync(channel, Retry30MinutesQueue, 1_800_000, cancellationToken);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, ImportRoutingKey,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareRetryAsync(IChannel channel, string queue, int ttl,
        CancellationToken cancellationToken)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = ttl,
            ["x-dead-letter-exchange"] = ImportExchange,
            ["x-dead-letter-routing-key"] = ImportRoutingKey
        };
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: arguments,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue, RetryExchange, queue, cancellationToken: cancellationToken);
    }
}