using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Pedidos;

public class CreateModel : PageModel
{
    private readonly OrderService _svc;
    public CreateModel(OrderService svc) => _svc = svc;
    [BindProperty] public Pedido Pedido { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _svc.CreateAsync(Pedido);
        return RedirectToPage("Index");
    }
}
