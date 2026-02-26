using Polly;
using Polly.Retry;
using Teste.Messaging;

namespace Teste.Workers;

public class ExternalApiWorker : BackgroundService
{
    private readonly MessageBus _bus;
    private readonly ILogger<ExternalApiWorker> _logger;
    private readonly ResiliencePipeline _retry;

    public ExternalApiWorker(MessageBus bus, ILogger<ExternalApiWorker> logger)
    {
        _bus = bus;
        _logger = logger;
        _retry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("[API] Retry {N} apos {D}s",
                        args.AttemptNumber + 1, args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExternalApiWorker iniciado.");
        await foreach (var msg in _bus.ReadAllAsync(stoppingToken))
        {
            await _retry.ExecuteAsync(async ct =>
            {
                _logger.LogInformation("[API] Chamando API para pedido {Id}...", msg.OrderId);
                await Task.Delay(500, ct);
                _logger.LogInformation("[API] Pedido {Id} confirmado pela API.", msg.OrderId);
            }, stoppingToken);
        }
    }
}
