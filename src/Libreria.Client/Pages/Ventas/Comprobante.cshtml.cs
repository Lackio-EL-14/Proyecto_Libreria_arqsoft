using System.Security.Claims;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

[Authorize(Policy = PoliticasAcceso.Ventas)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ComprobanteModel : PageModel
{
    private readonly IComprobanteVentaService _comprobantes;
    private readonly ILogger<ComprobanteModel> _logger;

    public ComprobanteModel(
        IComprobanteVentaService comprobantes,
        ILogger<ComprobanteModel> logger)
    {
        _comprobantes = comprobantes;
        _logger = logger;
    }

    public ComprobanteVenta? Comprobante { get; private set; }
    public string? MensajeError { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid publicId)
    {
        if (!ModelState.IsValid || publicId == Guid.Empty)
        {
            return ComprobanteNoEncontrado();
        }

        var generadoPor = User.FindFirst(ClaimTypes.GivenName)?.Value;
        if (string.IsNullOrWhiteSpace(generadoPor))
        {
            generadoPor = User.Identity?.Name;
        }

        if (User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(generadoPor))
        {
            return Challenge();
        }

        try
        {
            Comprobante = await _comprobantes.ObtenerAsync(publicId, generadoPor);
            return Comprobante is null ? ComprobanteNoEncontrado() : Page();
        }
        catch (Exception error)
        {
            _logger.LogError(error, "No se pudo consultar el comprobante de la venta {PublicId}.", publicId);
            MensajeError = "No se pudo cargar el comprobante. Intente abrirlo nuevamente.";
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Page();
        }
    }

    private IActionResult ComprobanteNoEncontrado()
    {
        MensajeError = "No se encontró la venta correspondiente a este comprobante.";
        Response.StatusCode = StatusCodes.Status404NotFound;
        return Page();
    }
}
