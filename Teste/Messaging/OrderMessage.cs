namespace Teste.Messaging;

public record OrderMessage(int OrderId, string CustomerName, string CustomerEmail, string Action);
