using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Libreria.Client.Services.Auditoria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Historico;

public class IndexModel : PageModel
{
    private readonly IProductosApi _productos;
    private readonly IAuditoriaService _auditoriaService;

    public IndexModel(
        IProductosApi productos,
        IAuditoriaService auditoriaService)
    {
        _productos = productos;
        _auditoriaService = auditoriaService;
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

        var historico = await _productos.ObtenerHistoricoAsync(publicId.Value);
        if (historico is null)
        {
            MensajeError = "El producto solicitado no fue encontrado o no está disponible.";
            return Page();
        }

        PublicId = publicId.Value;
        NombreProducto = historico.Nombre;
        ProductoActivo = historico.Activo;
        Historial =
            await _auditoriaService
                .ResolverUsuariosHistoricoAsync(
                    historico.Historial);

        return Page();
    }
}
