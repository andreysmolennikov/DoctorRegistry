namespace DoctorRegistry.Infrastructure.Persistence.Messaging;

/// <summary>
/// Persistence record used to make asynchronous message handling idempotent.
/// It is infrastructure state, not a domain entity.
/// </summary>
public sealed class ProcessedMessage
{
    public Guid MessageId { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
