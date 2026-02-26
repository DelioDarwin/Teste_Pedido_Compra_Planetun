using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Itens;

public class IndexModel : PageModel
{
    private readonly OrderService _svc;
    public IndexModel(OrderService svc) => _svc = svc;
    public Pedido? Pedido { get; set; }
    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        Pedido = await _svc.GetByIdAsync(orderId);
        return Pedido is null ? NotFound() : Page();
    }
}
