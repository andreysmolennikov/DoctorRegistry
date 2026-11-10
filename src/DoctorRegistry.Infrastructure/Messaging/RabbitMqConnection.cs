using RabbitMQ.Client;

namespace DoctorRegistry.Infrastructure.Messaging;

public sealed class RabbitMqConnection(RabbitMqOptions options) : IAsyncDisposable
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return _connection;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            Exception? last = null;
            foreach (var delay in new[]
                         { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) })
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken);
                try
                {
                    return _connection = await _factory.CreateConnectionAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    last = ex;
                }
            }

            throw new InvalidOperationException("Unable to establish RabbitMQ connection after bounded retries.", last);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
