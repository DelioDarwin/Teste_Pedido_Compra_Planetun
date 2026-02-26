using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Data;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Itens;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly OrderService _svc;
    public DeleteModel(AppDbContext db, OrderService svc) { _db = db; _svc = svc; }
    [BindProperty] public ItemPedido Item { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _db.ItensPedido.FindAsync(id);
        if (item is null) return NotFound();
        Item = item;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var item = await _db.ItensPedido.FindAsync(Item.ItemId);
        if (item is not null)
        {
            var orderId = item.OrderId;
            _db.ItensPedido.Remove(item);
            await _db.SaveChangesAsync();
            await _svc.RecalculateTotalAsync(orderId);
            return RedirectToPage("Index", new { orderId });
        }
        return RedirectToPage("../Pedidos/Index");
    }
}
