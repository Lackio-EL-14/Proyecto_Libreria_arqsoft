
using System.Text.Json;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Ventas;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

public class RegistrarModel : PageModel
{
    private readonly IServicioVentasClient _ventasApi;
    private readonly ILogger<RegistrarModel> _logger;

    public RegistrarModel(
        IServicioVentasClient ventasApi,
        ILogger<RegistrarModel> logger)
    {
        _ventasApi = ventasApi;
        _logger = logger;
    }

    [BindProperty]
    public ClienteInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetBuscarProductosAsync(string? nombre)
    {
        var ruta = "api/ventas/busquedas/productos?nombre="
            + Uri.EscapeDataString(nombre ?? string.Empty);

        var productos = await _ventasApi
            .GetAsync<List<ProductoVentaBusquedaDto>>(ruta);

        return new JsonResult(productos ?? []);
    }

    public async Task<IActionResult> OnGetBuscarClientesAsync(string? ciNit)
    {
        var ruta = "api/ventas/busquedas/clientes?ciNit="
            + Uri.EscapeDataString(ciNit ?? string.Empty);

        var clientes = await _ventasApi
            .GetAsync<List<ClienteVentaBusquedaDto>>(ruta);

        return new JsonResult(clientes ?? []);
    }

    public async Task<IActionResult> OnPostCrearClienteAsync()
    {
        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(campo => campo.Value?.Errors.Count > 0)
                .ToDictionary(
                    campo => campo.Key,
                    _ => "Revise el valor ingresado.");

            return new JsonResult(ClienteRegistroResult.Invalido(errores))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        try
        {
            var resultado = await _ventasApi
                .PostAsync<ClienteInput, ClienteRegistroResult>(
                    "api/clientes", Input);

            if (resultado is null)
            {
                return ErrorCliente(
                    "El servicio no devolvió el resultado del registro.",
                    StatusCodes.Status502BadGateway);
            }

            return new JsonResult(resultado)
            {
                StatusCode = resultado.Exitoso
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status400BadRequest
            };
        }
        catch (ApiClientException error)
        {
            _logger.LogWarning(error, "La API rechazó el registro del cliente.");

            return ErrorCliente(
                "No se pudo registrar el cliente.",
                StatusCodes.Status400BadRequest);
        }
        catch (Exception error)
        {
            _logger.LogError(error, "Error al registrar el cliente.");

            return ErrorCliente(
                "El servicio de clientes no está disponible.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task<IActionResult> OnPostGuardarVentaAsync(
        [FromBody] RegistrarVentaInput? venta)
    {
        if (!ModelState.IsValid ||
            venta is null ||
            venta.SolicitudId == Guid.Empty)
        {
            return ErrorVenta(
                "Revise los datos de la venta e intente nuevamente.",
                false);
        }

        try
        {
            var registrada = await _ventasApi
                .PostAsync<RegistrarVentaInput, JsonElement>(
                    "api/ventas", venta);

            return new JsonResult(new
            {
                exitoso = true,
                venta = registrada
            });
        }
        catch (ApiClientException error)
        {
            _logger.LogWarning(
                error,
                "La API rechazó la venta {SolicitudId}.",
                venta.SolicitudId);

            return ErrorVenta(
                "No se pudo registrar la venta. Revise los datos.",
                false);
        }
        catch (Exception error)
        {
            _logger.LogError(
                error,
                "Error al registrar la venta {SolicitudId}.",
                venta.SolicitudId);

            return ErrorVenta(
                "No se pudo confirmar el resultado de la venta. Reintente con la misma solicitud para evitar duplicados.",
                true);
        }
    }

    private static JsonResult ErrorCliente(
        string mensaje,
        int codigo)
    {
        var errores = new Dictionary<string, string>
        {
            ["General"] = mensaje
        };

        return new JsonResult(ClienteRegistroResult.Invalido(errores))
        {
            StatusCode = codigo
        };
    }

    private static JsonResult ErrorVenta(
        string mensaje,
        bool reintentarMismaSolicitud) =>
        new(new
        {
            exitoso = false,
            mensaje,
            reintentarMismaSolicitud
        })
        {
            StatusCode = reintentarMismaSolicitud
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status400BadRequest
        };
}
