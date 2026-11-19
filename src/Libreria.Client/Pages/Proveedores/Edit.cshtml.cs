using Libreria.Client.Adapters.Proveedores;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Proveedores;
using Libreria.Client.Services.Auditoria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Proveedores;

public sealed class EditModel : PageModel
{
    private readonly IProveedoresApi _proveedores;
    private readonly IAuditoriaService _auditoria;

    public EditModel(
        IProveedoresApi proveedores,
        IAuditoriaService auditoria)
    {
        _proveedores = proveedores;
        _auditoria = auditoria;
    }

    [BindProperty]
    public ProveedorInput Input { get; set; } = new();

    public AuditoriaRegistroDto? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var proveedor = await _proveedores.ObtenerProveedorAsync(id);
        if (proveedor is null)
        {
            return NotFound();
        }

        Input = MapearInput(proveedor);
        await CargarAuditoriaAsync(proveedor);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var resultado = await _proveedores.ActualizarProveedorAsync(id, Input);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            ModelState.AgregarErrores(resultado.Errores);
            var proveedor = await _proveedores.ObtenerProveedorAsync(id);
            if (proveedor is not null)
            {
                await CargarAuditoriaAsync(proveedor);
            }

            return Page();
        }

        TempData["MensajeExito"] = "Proveedor actualizado correctamente.";
        return RedirectToPage("./Index");
    }

    private static ProveedorInput MapearInput(ProveedorDetalleDto proveedor) => new()
    {
        Nit = proveedor.Nit,
        RazonSocial = proveedor.RazonSocial,
        Telefono = proveedor.Telefono,
        Correo = proveedor.Correo,
        NombreContacto = proveedor.NombreContacto ?? string.Empty,
        CargoContacto = proveedor.CargoContacto,
        TelefonoContacto = proveedor.TelefonoContacto,
        CorreoContacto = proveedor.CorreoContacto
    };

    private async Task CargarAuditoriaAsync(ProveedorDetalleDto proveedor)
    {
        Auditoria = await _auditoria.ObtenerAsync(
            proveedor.UsuarioCreacionId,
            proveedor.FechaCreacion,
            proveedor.UsuarioModificacionId,
            proveedor.FechaModificacion);
    }
}
