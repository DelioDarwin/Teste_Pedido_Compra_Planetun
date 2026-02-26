using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Pedidos;

public class EditModel : PageModel
{
    private readonly OrderService _svc;
    public EditModel(OrderService svc) => _svc = svc;
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
        if (!ModelState.IsValid) return Page();
        await _svc.UpdateAsync(Pedido);
        return RedirectToPage("Index");
    }
}
