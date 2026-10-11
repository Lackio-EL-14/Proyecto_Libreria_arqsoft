using Libreria.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Historico;

public class IndexModel : PageModel
{
    private readonly IHistoricoCostoRepository _repository;

    public IndexModel(IHistoricoCostoRepository repository)
    {
        _repository = repository;
    }

    public Guid PublicId { get; private set; }
    public string? NombreProducto { get; private set; }
    public bool ProductoActivo { get; private set; }
    public IReadOnlyList<HistoricoCostoItem> Historial { get; private set; } = [];
    public string? MensajeError { get; private set; }

    public IActionResult OnGet(Guid? publicId)
    {
        if (!publicId.HasValue || publicId.Value == Guid.Empty)
        {
            MensajeError = "Debe seleccionar un producto desde el catálogo de inventario para consultar su histórico de costos.";
            return Page();
        }

        var producto = _repository.ObtenerProducto(publicId.Value);

        if (producto is null)
        {
            MensajeError = "El producto solicitado no fue encontrado o no está disponible.";
            return Page();
        }

        PublicId = publicId.Value;
        NombreProducto = producto.Nombre;
        ProductoActivo = producto.Estado;
        Historial = _repository.ObtenerHistorico(producto.ProductoId);

        return Page();
    }
}
