namespace DoctorRegistry.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = "/";
    public ushort PrefetchCount { get; init; } = 10;
    public int RetryPublishConfirmTimeoutSeconds { get; init; } = 10;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(UserName) ||
            string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("RabbitMq Host, UserName and Password are required.");
        if (Port is < 1 or > 65535 || PrefetchCount == 0 || RetryPublishConfirmTimeoutSeconds <= 0)
            throw new InvalidOperationException("RabbitMq numeric settings are invalid.");
    }
}