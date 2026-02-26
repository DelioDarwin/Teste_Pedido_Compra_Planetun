using Microsoft.EntityFrameworkCore;
using Teste.Models;

namespace Teste.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(e =>
        {
            e.ToTable("pedidos");
            e.HasMany(p => p.Itens)
             .WithOne(i => i.Pedido)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemPedido>(e =>
        {
            e.ToTable("itens_pedido");
        });
    }
}
