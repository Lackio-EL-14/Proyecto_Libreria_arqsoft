using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public IndexModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    public IReadOnlyList<ProductoListItemDto> Productos { get; private set; } = [];
    public IReadOnlyList<CategoriaOptionDto> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOptionDto> Marcas { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoriaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MarcaId { get; set; }

    public async Task OnGetAsync()
    {
        var listado = await _ventas.ObtenerProductosAsync(Busqueda, CategoriaId, MarcaId);

        Productos = listado.Productos;
        Categorias = listado.Categorias;
        Marcas = listado.Marcas;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _ventas.DarDeBajaProductoAsync(publicId);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }
}
