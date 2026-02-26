using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Pedidos;

public class DeleteModel : PageModel
{
    private readonly OrderService _svc;
    public DeleteModel(OrderService svc) => _svc = svc;
    [BindProperty] public Pedido Pedido { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await _svc.GetByIdAsync(id);
        if (p is null) return NotFound();
        Pedido = p;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        await _svc.DeleteAsync(Pedido.OrderId);
        return RedirectToPage("Index");
    }
}
