using Libreria.Client.DTOs.Common;
using Libreria.Client.Clients.Productos;
using Libreria.Client.DTOs.Productos;
using Libreria.Client.Seguridad;
using Libreria.Client.Services.Auditoria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Categorias;

public class EditModel : PageModel
{
    private readonly IServicioProductosClient _productos;
    private readonly IUrlProtector _urlProtector;
    private readonly IAuditoriaService _auditoriaService;

    public EditModel(
        IServicioProductosClient productos,
        IUrlProtector urlProtector,
        IAuditoriaService auditoriaService)
    {
        _productos = productos;
        _urlProtector = urlProtector;
        _auditoriaService = auditoriaService;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public IReadOnlyList<string> Ubicaciones { get; private set; } = [];

    public AuditoriaRegistroDto? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var publicIdDescifrado = _urlProtector.Descifrar(id);
        if (publicIdDescifrado is null)
        {
            return NotFound();
        }

        var categoria = await _productos.ObtenerCategoriaAsync(publicIdDescifrado.Value, true);
        if (categoria is null)
        {
            return NotFound();
        }

        PublicId = categoria.PublicId;
        Input = new CategoriaInput
        {
            Codigo = categoria.Codigo,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Ubicacion = categoria.Ubicacion
        };

        Auditoria = await _auditoriaService.ObtenerAsync(
            categoria.UsuarioCreacionId,
            categoria.FechaCreacion,
            categoria.UsuarioModificacionId,
            categoria.FechaModificacion);

        await CargarUbicacionesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _productos.ActualizarCategoriaAsync(PublicId, Input);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            await CargarUbicacionesAsync();
            return Page();
        }

        TempData["MensajeExito"] = "Categoría actualizada correctamente.";
        return RedirectToPage("./Index");
    }

    private async Task CargarUbicacionesAsync()
    {
        Ubicaciones = await _productos.ObtenerUbicacionesCategoriaAsync();
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
