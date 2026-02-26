using Microsoft.AspNetCore.Mvc.RazorPages;
using Teste.Models;
using Teste.Services;

namespace Teste.Pages.Pedidos;

public class IndexModel : PageModel
{
    private readonly OrderService _svc;
    public IndexModel(OrderService svc) => _svc = svc;
    public List<Pedido> Pedidos { get; set; } = [];
    public async Task OnGetAsync() { Pedidos = await _svc.GetAllAsync(); }
}
