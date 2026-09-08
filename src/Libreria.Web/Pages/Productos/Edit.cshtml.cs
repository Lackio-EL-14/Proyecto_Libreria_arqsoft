using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class EditModel : PageModel
{
    private readonly IEdicionProductoService _service;

    public EditModel(IEdicionProductoService service)
    {
        _service = service;
    }

    [BindProperty]
    public int ProductoId { get; set; }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    public IActionResult OnGet(int id)
    {
        var edicion = _service.ObtenerEdicion(id);
        if (edicion is null)
        {
            return NotFound();
        }

        ProductoId = edicion.ProductoId;
        Input = edicion.Input;
        Categorias = edicion.Categorias;
        Marcas = edicion.Marcas;
        return Page();
    }

    public IActionResult OnPost()
    {
        var resultado = _service.Actualizar(ProductoId, Input);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            CargarOpciones();
            return Page();
        }

        TempData["MensajeExito"] = "Producto actualizado correctamente.";
        return RedirectToPage("Details", new { id = ProductoId });
    }

    private void CargarOpciones()
    {
        var edicion = _service.ObtenerEdicion(ProductoId);
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
