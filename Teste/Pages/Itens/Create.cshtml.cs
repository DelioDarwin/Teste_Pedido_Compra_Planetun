using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Data;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Itens;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly OrderService _svc;
    public CreateModel(AppDbContext db, OrderService svc) { _db = db; _svc = svc; }
    [BindProperty] public ItemPedido Item { get; set; } = new();
    public int OrderId { get; set; }
    public void OnGet(int orderId) { OrderId = orderId; Item.OrderId = orderId; }
    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Item.Pedido");
        if (!ModelState.IsValid) { OrderId = Item.OrderId; return Page(); }
        _db.ItensPedido.Add(Item);
        await _db.SaveChangesAsync();
        await _svc.RecalculateTotalAsync(Item.OrderId);
        return RedirectToPage("Index", new { orderId = Item.OrderId });
    }
}
