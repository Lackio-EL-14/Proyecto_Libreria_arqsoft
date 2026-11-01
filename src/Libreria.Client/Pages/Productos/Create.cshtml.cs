using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public CreateModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
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
        var resultado = await _ventas.RegistrarProductoAsync(Input);

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
        var formulario = await _ventas.ObtenerFormularioProductoAsync();
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
