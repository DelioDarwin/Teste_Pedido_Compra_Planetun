using Polly;
using Polly.Retry;
using Teste.Messaging;
using Teste.Services;

namespace Teste.Workers;

public class EmailWorker : BackgroundService
{
    private readonly MessageBus _bus;
    private readonly ILogger<EmailWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ResiliencePipeline _retry;

    public EmailWorker(MessageBus bus, ILogger<EmailWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _bus = bus;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _retry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("[EMAIL] Retry {N} apos {D}s: {Err}",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalSeconds,
                        args.Outcome.Exception?.Message ?? "desconhecido");
                    return ValueTask.CompletedTask;
                }
            }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailWorker iniciado.");
        await foreach (var msg in _bus.ReadAllAsync(stoppingToken))
        {
            await _retry.ExecuteAsync(async ct =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var body = $"""
                    <h2>Pedido #{msg.OrderId} Confirmado!</h2>
                    <p>Olá <strong>{msg.CustomerName}</strong>,</p>
                    <p>Seu pedido foi recebido com sucesso.</p>
                    <p>Obrigado por comprar conosco!</p>
                    """;

                await emailService.SendAsync(
                    msg.CustomerEmail,
                    msg.CustomerName,
                    $"Pedido #{msg.OrderId} - Confirmação",
                    body,
                    ct);

            }, stoppingToken);
        }
    }
}