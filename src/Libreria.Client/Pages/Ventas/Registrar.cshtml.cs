using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

public class RegistrarModel : PageModel
{
    private readonly IConsultaVentaService _consulta;
    private readonly IClienteService _clientes;
    private readonly IVentaFacade _ventas;
    private readonly ILogger<RegistrarModel> _logger;

    public RegistrarModel(
        IConsultaVentaService consulta,
        IClienteService clientes,
        IVentaFacade ventas,
        ILogger<RegistrarModel> logger)
    {
        _consulta = consulta;
        _clientes = clientes;
        _ventas = ventas;
        _logger = logger;
    }

    [BindProperty]
    public ClienteInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetBuscarProductosAsync(string? nombre)
    {
        return new JsonResult(await _consulta.BuscarProductosAsync(nombre));
    }

    public async Task<IActionResult> OnGetBuscarClientesAsync(string? ciNit)
    {
        return new JsonResult(await _consulta.BuscarClientesAsync(ciNit));
    }

    public async Task<IActionResult> OnPostCrearClienteAsync()
    {
        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(campo => campo.Value?.Errors.Count > 0)
                .ToDictionary(campo => campo.Key, _ => "Revise el valor ingresado.");
            return new JsonResult(ClienteRegistroResult.Invalido(errores))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        var resultado = await _clientes.RegistrarRapidoAsync(Input);
        return new JsonResult(resultado)
        {
            StatusCode = resultado.Exitoso
                ? StatusCodes.Status200OK
                : StatusCodes.Status400BadRequest
        };
    }

    public async Task<IActionResult> OnPostGuardarVentaAsync([FromBody] RegistrarVentaInput? venta)
    {
        // Validar el formato de la petición; las reglas y la transacción pertenecen a la fachada.
        if (!ModelState.IsValid || venta is null || venta.SolicitudId == Guid.Empty)
        {
            return ErrorVenta("Revise los datos de la venta e intente nuevamente.", false);
        }

        try
        {
            var registrada = await _ventas.RegistrarVentaAsync(venta);
            return new JsonResult(new { exitoso = true, venta = registrada });
        }
        catch (VentaRegistroException error)
        {
            _logger.LogWarning(error, "No se completó la solicitud de venta {SolicitudId}.", venta.SolicitudId);
            return ErrorVenta(error.Message, error.ResultadoIncierto);
        }
        catch (ArgumentException error)
        {
            _logger.LogWarning(error, "Datos inválidos en la solicitud de venta {SolicitudId}.", venta.SolicitudId);
            return ErrorVenta("Seleccione un cliente y revise los productos y cantidades de la venta.", false);
        }
        catch (Exception error)
        {
            _logger.LogError(error, "No se pudo verificar el resultado de la solicitud de venta {SolicitudId}.", venta.SolicitudId);
            return ErrorVenta(
                "No se pudo confirmar el resultado de la venta. Reintente el guardado para comprobarlo sin duplicarla.",
                true);
        }
    }

    private static JsonResult ErrorVenta(string mensaje, bool reintentarMismaSolicitud) =>
        new(new { exitoso = false, mensaje, reintentarMismaSolicitud })
        {
            StatusCode = reintentarMismaSolicitud
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status400BadRequest
        };
}
