using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class EditModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public EditModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    public Guid PublicId { get; private set; }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOptionDto> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOptionDto> Marcas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var edicion = await _ventas.ObtenerProductoParaEdicionAsync(id);
        if (edicion is null)
        {
            return NotFound();
        }

        PublicId = id;
        Input = edicion.Input;
        Categorias = edicion.Categorias;
        Marcas = edicion.Marcas;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var resultado = await _ventas.ActualizarProductoAsync(id, Input);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            PublicId = id;
            await CargarOpcionesAsync(id);
            return Page();
        }

        TempData["MensajeExito"] = "Producto actualizado correctamente.";

        return RedirectToPage("Details", new { id });
    }

    private async Task CargarOpcionesAsync(Guid publicId)
    {
        var edicion = await _ventas.ObtenerProductoParaEdicionAsync(publicId);
        if (edicion is null)
        {
            return;
        }

        Categorias = edicion.Categorias;
        Marcas = edicion.Marcas;
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
