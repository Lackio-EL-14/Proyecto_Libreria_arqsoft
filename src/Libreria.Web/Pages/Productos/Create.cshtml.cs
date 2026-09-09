using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly IRegistroProductoService _service;

    public CreateModel(IRegistroProductoService service)
    {
        _service = service;
    }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    public void OnGet()
    {
        CargarFormulario();
    }

    public IActionResult OnPost()
    {
        var resultado = _service.Registrar(Input);
        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            CargarFormulario();
            return Page();
        }

        TempData["MensajeExito"] = "Producto registrado correctamente.";
        return RedirectToPage("Index");
    }

    private void CargarFormulario()
    {
        var formulario = _service.ObtenerFormulario();
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
