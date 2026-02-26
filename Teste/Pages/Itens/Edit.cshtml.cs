using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Data;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Itens;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly OrderService _svc;
    public EditModel(AppDbContext db, OrderService svc) { _db = db; _svc = svc; }
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
        ModelState.Remove("Item.Pedido");
        if (!ModelState.IsValid) return Page();
        _db.ItensPedido.Update(Item);
        await _db.SaveChangesAsync();
        await _svc.RecalculateTotalAsync(Item.OrderId);
        return RedirectToPage("Index", new { orderId = Item.OrderId });
    }
}
