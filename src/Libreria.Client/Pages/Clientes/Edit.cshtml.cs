using Libreria.Client.Clients.Clientes;
using Libreria.Client.DTOs.Clientes;
using Libreria.Client.DTOs.Common;
using Libreria.Client.Services.Auditoria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Clientes;

public class EditModel : PageModel
{
    private readonly IServicioClientesClient _clientes;
    private readonly IAuditoriaService _auditoria;

    public EditModel(IServicioClientesClient clientes, IAuditoriaService auditoria)
    {
        _clientes = clientes;
        _auditoria = auditoria;
    }

    [BindProperty]
    public ClienteInput Input { get; set; } = new();

    public AuditoriaRegistroDto? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var cliente = await _clientes.ObtenerClienteAsync(id);
        if (cliente is null)
        {
            return NotFound();
        }

        Input = new ClienteInput
        {
            CiNit = cliente.CiNit,
            RazonSocial = cliente.RazonSocial,
            Correo = cliente.Correo
        };

        await CargarAuditoriaAsync(cliente);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var resultado = await _clientes.ActualizarClienteAsync(id, Input);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            ModelState.AgregarErrores(resultado.Errores);
            var cliente = await _clientes.ObtenerClienteAsync(id);
            if (cliente is not null)
            {
                await CargarAuditoriaAsync(cliente);
            }

            return Page();
        }

        TempData["MensajeExito"] = "Cliente actualizado correctamente.";
        return RedirectToPage("./Index");
    }

    private async Task CargarAuditoriaAsync(ClienteDetalleDto cliente)
    {
        Auditoria = await _auditoria.ObtenerAsync(
            cliente.UsuarioCreacionId,
            cliente.FechaCreacion,
            cliente.UsuarioModificacionId,
            cliente.FechaModificacion);
    }
}
