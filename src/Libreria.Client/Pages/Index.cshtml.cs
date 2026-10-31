using Libreria.Client.Clients;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages;

public class IndexModel : PageModel
{
    private const int UmbralStockCritico = 5;

    private readonly IVentasApiClient _ventas;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(
        IVentasApiClient ventas,
        IAuthorizationService authorizationService)
    {
        _ventas = ventas;
        _authorizationService = authorizationService;
    }

    public int TotalProductos { get; private set; }
    public int ProductosStockCritico { get; private set; }
    public int TotalCategorias { get; private set; }
    public int TotalMarcas { get; private set; }
    public int TotalPerecederos { get; private set; }
    public bool EsAdministrador { get; private set; }

    public async Task OnGetAsync()
    {
        EsAdministrador = (await _authorizationService
            .AuthorizeAsync(User, PoliticasAcceso.SoloAdministrador)).Succeeded;

        if (!EsAdministrador)
        {
            return;
        }

        try
        {
            // Un solo llamado al servicio trae productos, categorías y marcas activas.
            var listado = await _ventas.ObtenerProductosAsync(null, null, null);

            TotalProductos = listado.Productos.Count;
            ProductosStockCritico = listado.Productos.Count(p => p.Stock <= UmbralStockCritico);
            TotalPerecederos = listado.Productos.Count(p => p.EsPerecedero);
            TotalCategorias = listado.Categorias.Count;
            TotalMarcas = listado.Marcas.Count;
        }
        catch (HttpRequestException)
        {
            // Si el servicio de Ventas no responde, el tablero se muestra en cero.
            TotalProductos = 0;
            ProductosStockCritico = 0;
            TotalPerecederos = 0;
            TotalCategorias = 0;
            TotalMarcas = 0;
        }
    }
}
