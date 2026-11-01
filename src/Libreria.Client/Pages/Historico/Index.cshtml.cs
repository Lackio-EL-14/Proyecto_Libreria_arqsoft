using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Historico;

public class IndexModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public IndexModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    public Guid PublicId { get; private set; }
    public string? NombreProducto { get; private set; }
    public bool ProductoActivo { get; private set; }
    public IReadOnlyList<HistoricoCostoDto> Historial { get; private set; } = [];
    public string? MensajeError { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? publicId)
    {
        if (!publicId.HasValue || publicId.Value == Guid.Empty)
        {
            MensajeError = "Debe seleccionar un producto desde el catálogo de inventario para consultar su histórico de costos.";
            return Page();
        }

        var historico = await _ventas.ObtenerHistoricoAsync(publicId.Value);
        if (historico is null)
        {
            MensajeError = "El producto solicitado no fue encontrado o no está disponible.";
            return Page();
        }

        PublicId = publicId.Value;
        NombreProducto = historico.Nombre;
        ProductoActivo = historico.Activo;
        Historial = historico.Historial;

        return Page();
    }
}
