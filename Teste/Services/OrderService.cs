using Microsoft.EntityFrameworkCore;
using Teste.Data;
using Teste.Messaging;
using Teste.Models;

namespace Teste.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly CompositeMessageBus _bus;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, CompositeMessageBus bus, ILogger<OrderService> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    public async Task<List<Pedido>> GetAllAsync()
        => await _db.Pedidos.Include(p => p.Itens)
            .OrderByDescending(p => p.OrderDate)
            .AsNoTracking().ToListAsync();

    public async Task<Pedido?> GetByIdAsync(int id)
        => await _db.Pedidos.Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.OrderId == id);

    public async Task<Pedido> CreateAsync(Pedido pedido)
    {
        pedido.OrderDate = DateTime.UtcNow;
        pedido.Status = "Pendente";
        pedido.TotalAmount = pedido.Itens.Sum(i => i.Quantity * i.UnitPrice);

        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Pedido {Id} salvo no banco.", pedido.OrderId);

        await _bus.PublishAsync(new OrderMessage(
            pedido.OrderId, pedido.CustomerName, pedido.CustomerEmail, "Criado"));
        _logger.LogInformation("Mensagem do pedido {Id} publicada.", pedido.OrderId);

        return pedido;
    }

    public async Task UpdateAsync(Pedido pedido)
    {
        var existing = await _db.Pedidos.Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.OrderId == pedido.OrderId);
        if (existing is null) return;

        existing.CustomerName = pedido.CustomerName;
        existing.CustomerEmail = pedido.CustomerEmail;
        existing.Status = pedido.Status;
        existing.TotalAmount = existing.Itens.Sum(i => i.Quantity * i.UnitPrice);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var pedido = await _db.Pedidos.FindAsync(id);
        if (pedido is null) return;
        _db.Pedidos.Remove(pedido);
        await _db.SaveChangesAsync();
    }

    public async Task RecalculateTotalAsync(int orderId)
    {
        var pedido = await _db.Pedidos.Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (pedido is null) return;
        pedido.TotalAmount = pedido.Itens.Sum(i => i.Quantity * i.UnitPrice);
        await _db.SaveChangesAsync();
    }
}
