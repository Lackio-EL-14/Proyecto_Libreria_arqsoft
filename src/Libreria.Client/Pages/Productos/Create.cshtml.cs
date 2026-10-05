using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace Libreria.Client.Pages.Productos;

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
