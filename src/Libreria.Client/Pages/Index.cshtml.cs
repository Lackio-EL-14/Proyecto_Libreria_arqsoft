using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages;

public class IndexModel : PageModel
{
    private readonly HttpClient _httpClient;

    public IndexModel(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("VentasAPI");
    }

    public int TotalProductos { get; private set; }
    public int TotalCategorias { get; private set; }
    public int TotalMarcas { get; private set; }
    public int TotalPerecederos { get; private set; }
    public int ProductosStockCritico { get; private set; }
    public bool EsAdministrador { get; private set; } = true; // Por ahora, asumimos admin para UI

    public async Task OnGetAsync()
    {
        TotalProductos = 0; TotalCategorias = 0; TotalMarcas = 0; TotalPerecederos = 0; ProductosStockCritico = 0;
    }
}
