using System.Threading.Channels;

namespace Teste.Messaging;

public class MessageBus
{
    private readonly Channel<OrderMessage> _channel = Channel.CreateUnbounded<OrderMessage>();

    public async ValueTask PublishAsync(OrderMessage message)
        => await _channel.Writer.WriteAsync(message);

    public IAsyncEnumerable<OrderMessage> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
