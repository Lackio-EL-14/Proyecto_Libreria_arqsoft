using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IProductosApi _productos;

    public IndexModel(IProductosApi productos)
    {
        _productos = productos;
    }

    public IReadOnlyList<ProductoListItemDto> Productos { get; private set; } = [];
    public IReadOnlyList<CategoriaOptionDto> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOptionDto> Marcas { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoriaPublicId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? MarcaPublicId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Estado { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Orden { get; set; } = "nombre";

    [BindProperty(SupportsGet = true)]
    public string Direccion { get; set; } = "asc";

    public async Task OnGetAsync()
    {
        var listado = await _productos.ObtenerProductosAsync(
            Busqueda,
            CategoriaPublicId,
            MarcaPublicId,
            Estado);

        Productos = Ordenar(listado.Productos);
        Categorias = listado.Categorias;
        Marcas = listado.Marcas;
    }

    public string SiguienteDireccion(string columna)
    {
        return string.Equals(
                   Orden,
                   columna,
                   StringComparison.OrdinalIgnoreCase)
               && !string.Equals(
                   Direccion,
                   "desc",
                   StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";
    }

    public string IndicadorOrden(string columna)
    {
        if (!string.Equals(
                Orden,
                columna,
                StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return string.Equals(
            Direccion,
            "desc",
            StringComparison.OrdinalIgnoreCase)
            ? " ▼"
            : " ▲";
    }

    private IReadOnlyList<ProductoListItemDto> Ordenar(
        IReadOnlyList<ProductoListItemDto> productos)
    {
        var descendente = string.Equals(
            Direccion,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        IEnumerable<ProductoListItemDto> ordenados =
            Orden.ToLowerInvariant() switch
            {
                "categoria" => descendente
                    ? productos.OrderByDescending(x => x.Categoria)
                    : productos.OrderBy(x => x.Categoria),

                "marca" => descendente
                    ? productos.OrderByDescending(x => x.Marca)
                    : productos.OrderBy(x => x.Marca),

                "perecedero" => descendente
                    ? productos.OrderByDescending(x => x.EsPerecedero)
                    : productos.OrderBy(x => x.EsPerecedero),

                "estado" => descendente
                    ? productos.OrderByDescending(x => x.Estado)
                    : productos.OrderBy(x => x.Estado),

                "stock" => descendente
                    ? productos.OrderByDescending(x => x.Stock)
                    : productos.OrderBy(x => x.Stock),

                "precio" => descendente
                    ? productos.OrderByDescending(x => x.PrecioVenta)
                    : productos.OrderBy(x => x.PrecioVenta),

                "costo" => descendente
                    ? productos.OrderByDescending(
                        x => x.CostoAdquisicionActual)
                    : productos.OrderBy(
                        x => x.CostoAdquisicionActual),

                _ => descendente
                    ? productos.OrderByDescending(x => x.Nombre)
                    : productos.OrderBy(x => x.Nombre)
            };

        return ordenados.ToList();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _productos.DarDeBajaProductoAsync(publicId);
        if (!resultado.Exitoso)
        {
            TempData["MensajeError"] = MensajeDeError(resultado, "No se pudo dar de baja el producto.");
            return RedirectToPage("./Index");
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostReactivarAsync(Guid publicId)
    {
        var resultado =
            await _productos.ReactivarProductoAsync(publicId);

        if (!resultado.Exitoso)
        {
            TempData["MensajeError"] = MensajeDeError(resultado, "No se pudo reactivar el producto.");
            return RedirectToPage("./Index", new { Estado = false });
        }

        TempData["MensajeExito"] =
            "Producto reactivado correctamente.";

        return RedirectToPage(
            "./Index",
            new { Estado = false });
    }

    /// <summary>Mensaje del Result del MS (409, 400, servicio caído…) en lugar de un 404 sin explicación.</summary>
    public static string MensajeDeError(ResultadoApi resultado, string porDefecto) =>
        resultado.NoEncontrado
            ? "El producto no existe."
            : resultado.Errores.Values.FirstOrDefault() ?? porDefecto;
}
