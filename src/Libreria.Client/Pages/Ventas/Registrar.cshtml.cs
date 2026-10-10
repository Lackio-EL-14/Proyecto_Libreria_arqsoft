using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

public class RegistrarModel : PageModel
{
    private readonly IConsultaVentaService _consulta;
    private readonly IClienteService _clientes;

    public RegistrarModel(IConsultaVentaService consulta, IClienteService clientes)
    {
        _consulta = consulta;
        _clientes = clientes;
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
}
