namespace Teste.Messaging;

public class CompositeMessageBus
{
    private readonly List<MessageBus> _subscribers = [];

    public void Subscribe(MessageBus bus) => _subscribers.Add(bus);

    public async ValueTask PublishAsync(OrderMessage message)
    {
        foreach (var bus in _subscribers)
            await bus.PublishAsync(message);
    }
}
