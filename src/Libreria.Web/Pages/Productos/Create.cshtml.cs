using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly IProductoService _service;
    private readonly ICrudRepository<Producto> _repository;

    public CreateModel(
        IProductoService service,
        CrudRepositoryFactory<Producto> factory)
    {
        _service = service;
        _repository = factory.CrearRepositorio();
    }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    public void OnGet()
    {
        CargarFormulario();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _service.RegistrarAsync(Input, _repository);

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
