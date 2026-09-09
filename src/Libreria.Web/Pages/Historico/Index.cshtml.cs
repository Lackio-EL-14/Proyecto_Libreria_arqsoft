using Libreria.Web.Pages.Historico.Models;
using Libreria.Web.Pages.Historico.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Historico;

public class IndexModel : PageModel
{
    private readonly IHistoricoCostoRepository _repository;

    public IndexModel(IHistoricoCostoRepository repository)
    {
        _repository = repository;
    }

    public int ProductoId { get; private set; }
    public string? NombreProducto { get; private set; }
    public bool ProductoActivo { get; private set; }
    public IReadOnlyList<HistoricoCostoItem> Historial { get; private set; } = [];
    public string? MensajeError { get; private set; }

    public IActionResult OnGet(int productoId)
    {
        if (productoId <= 0)
        {
            MensajeError = "Debe seleccionar un producto válido.";
            return Page();
        }

        ProductoId = productoId;
        var producto = _repository.ObtenerProducto(productoId);
        if (producto is null)
        {
            MensajeError = "El producto solicitado no existe.";
            return Page();
        }

        NombreProducto = producto.Nombre;
        ProductoActivo = producto.Estado;
        Historial = _repository.ObtenerHistorico(productoId);
        return Page();
    }
}
