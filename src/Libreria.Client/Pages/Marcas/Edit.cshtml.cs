using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class EditModel : PageModel
{
    private readonly IVentasApiClient _ventas;
    private readonly IAuditoriaService _auditoriaService;

    public EditModel(
        IVentasApiClient ventas,
        IAuditoriaService auditoriaService)
    {
        _ventas = ventas;
        _auditoriaService = auditoriaService;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public IReadOnlyList<string> Paises { get; private set; } = [];

    public AuditoriaRegistro? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var marca = await _ventas.ObtenerMarcaAsync(id, true);
        if (marca is null)
        {
            return NotFound();
        }

        PublicId = marca.PublicId;

        Input = new MarcaInput
        {
            Nombre = marca.Nombre,
            Descripcion = marca.Descripcion,
            PaisOrigen = marca.PaisOrigen,
            SitioWeb = marca.SitioWeb
        };

        Auditoria = await _auditoriaService.ObtenerAsync(
            marca.UsuarioCreacionId,
            null,
            marca.UsuarioModificacionId,
            null);

        await CargarPaisesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _ventas.ActualizarMarcaAsync(PublicId, Input);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            await CargarPaisesAsync();
            return Page();
        }

        TempData["MensajeExito"] = "Marca actualizada correctamente.";
        return RedirectToPage("./Index");
    }

    private async Task CargarPaisesAsync()
    {
        Paises = await _ventas.ObtenerPaisesMarcaAsync();
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
