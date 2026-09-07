using Libreria.Web.Data;
using Libreria.Web.Pages.Historico.Models;
using Libreria.Web.Pages.Historico.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Historico;

public class IndexModel : PageModel
{
    private readonly HistoricoCostoRepository _repository;

    public IndexModel(IDbConnectionFactory connectionFactory)
    {
        _repository = new HistoricoCostoRepository(connectionFactory);
    }

    public int ProductoId { get; private set; }

    public string? NombreProducto { get; private set; }

    public List<HistoricoCostoItem> Historial { get; private set; } = new();

    public string? MensajeError { get; private set; }

    public IActionResult OnGet(int productoId)
    {
        if (productoId <= 0)
        {
            MensajeError = "Debe seleccionar un producto válido.";
            return Page();
        }

        ProductoId = productoId;

        NombreProducto = _repository.ObtenerNombreProducto(productoId);

        if (NombreProducto == null)
        {
            MensajeError = "El producto solicitado no existe.";
            return Page();
        }

        Historial = _repository.ObtenerHistorico(productoId);

        return Page();
    }
}