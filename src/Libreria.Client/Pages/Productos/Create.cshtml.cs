using Libreria.Client.Clients.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly IServicioProductosClient _productos;

    public CreateModel(IServicioProductosClient productos)
    {
        _productos = productos;
    }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOptionDto> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOptionDto> Marcas { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarFormularioAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _productos.RegistrarProductoAsync(Input);

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            await CargarFormularioAsync();
            return Page();
        }

        TempData["MensajeExito"] = "Producto registrado correctamente.";
        return RedirectToPage("Index");
    }

    private async Task CargarFormularioAsync()
    {
        var formulario = await _productos.ObtenerFormularioProductoAsync();
        Categorias = formulario.Categorias;
        Marcas = formulario.Marcas;
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
