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

    public Guid PublicId { get; private set; }
    public string? NombreProducto { get; private set; }
    public bool ProductoActivo { get; private set; }
    public IReadOnlyList<HistoricoCostoItem> Historial { get; private set; } = [];
    public string? MensajeError { get; private set; }

    public IActionResult OnGet(Guid? publicId)
    {
        if (!publicId.HasValue || publicId.Value == Guid.Empty)
        {
            return NotFound();
        }

        var producto = _repository.ObtenerProducto(publicId.Value);

        if (producto is null)
        {
            return NotFound();
        }

        PublicId = publicId.Value;
        NombreProducto = producto.Nombre;
        ProductoActivo = producto.Estado;
        Historial = _repository.ObtenerHistorico(producto.ProductoId);

        return Page();
    }
}
