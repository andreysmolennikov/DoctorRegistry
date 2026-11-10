using System.Text.Json;
using DoctorRegistry.Application.Imports;
using DoctorRegistry.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DoctorRegistry.Worker;

public sealed class PractitionerImportConsumer(
    RabbitMqConnection connection,
    RabbitMqTopology topology,
    RabbitMqOptions options,
    IServiceScopeFactory scopes,
    ILogger<PractitionerImportConsumer> logger) : BackgroundService
{
    private IChannel? _channel;
    private string? _consumerTag;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delays = new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };
        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await topology.EnsureAsync(stoppingToken);
                var channel =
                    await (await connection.GetAsync(stoppingToken)).CreateChannelAsync(
                        cancellationToken: stoppingToken);
                _channel = channel;
                var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                channel.ChannelShutdownAsync += (_, _) =>
                {
                    shutdown.TrySetResult();
                    return Task.CompletedTask;
                };
                await channel.BasicQosAsync(0, options.PrefetchCount, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) => ProcessAsync(channel, delivery, stoppingToken);
                consumer.ShutdownAsync += (_, _) =>
                {
                    shutdown.TrySetResult();
                    return Task.CompletedTask;
                };
                _consumerTag = await channel.BasicConsumeAsync(RabbitMqTopology.ImportQueue, autoAck: false, consumer,
                    stoppingToken);
                attempt = 0;
                await Task.WhenAny(shutdown.Task, Task.Delay(Timeout.Infinite, stoppingToken));
                if (!stoppingToken.IsCancellationRequested)
                    logger.LogWarning("RabbitMQ consumer channel stopped; recreating it.");
                await channel.DisposeAsync();
                _channel = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (stoppingToken.IsCancellationRequested) break;
                var delay = delays[Math.Min(++attempt, delays.Length - 1)];
                logger.LogError(ex, "RabbitMQ consumer recovery failed; retrying in {Delay}.", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(IChannel deliveryChannel, BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<PractitionerImportMessageV1>(delivery.Body.Span)
                          ?? throw new JsonException("Empty practitioner import message.");
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ImportPractitionerHandler>();
            await handler.Handle(message, stoppingToken);
            if (!stoppingToken.IsCancellationRequested && deliveryChannel.IsOpen)
                await deliveryChannel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            var retry = IsPermanent(ex) ? null : NextRetry(delivery.BasicProperties.Headers);
            try
            {
                await ForwardAsync(delivery, retry, stoppingToken);
                if (deliveryChannel.IsOpen)
                    await deliveryChannel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
            }
            catch (Exception forwardEx)
            {
                logger.LogError(forwardEx,
                    "Unable to forward practitioner import {MessageId}; original delivery remains unacknowledged.",
                    delivery.BasicProperties.MessageId);
            }
        }
    }

    private async Task ForwardAsync(BasicDeliverEventArgs delivery, string? retryQueue,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RetryPublishConfirmTimeoutSeconds));
        await using var channel = await (await connection.GetAsync(timeout.Token)).CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            timeout.Token);
        var attempts = GetAttempts(delivery.BasicProperties.Headers) + 1;
        var headers = delivery.BasicProperties.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(delivery.BasicProperties.Headers);
        headers["retry-attempt"] = attempts;
        var properties = new BasicProperties
        {
            MessageId = delivery.BasicProperties.MessageId,
            ContentType = delivery.BasicProperties.ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            Headers = headers
        };
        var exchange = retryQueue is null ? RabbitMqTopology.DeadLetterExchange : RabbitMqTopology.RetryExchange;
        var key = retryQueue ?? RabbitMqTopology.ImportRoutingKey;
        await channel.BasicPublishAsync(exchange, key, mandatory: true, properties, delivery.Body, timeout.Token);
    }

    private static bool IsPermanent(Exception exception) => exception is JsonException or ValidationException;

    private static string? NextRetry(IDictionary<string, object?>? headers) => GetAttempts(headers) switch
    {
        0 => RabbitMqTopology.Retry30SecondsQueue,
        1 => RabbitMqTopology.Retry5MinutesQueue,
        2 => RabbitMqTopology.Retry30MinutesQueue,
        _ => null
    };

    private static int GetAttempts(IDictionary<string, object?>? headers) =>
        headers?.TryGetValue("retry-attempt", out var value) == true &&
        int.TryParse(value?.ToString(), out var attempts)
            ? attempts
            : 0;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null && _consumerTag is not null && _channel.IsOpen)
            await _channel.BasicCancelAsync(_consumerTag, false, cancellationToken);
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
    }
}