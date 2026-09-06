using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Pages.Productos.Services;
using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;

namespace Libreria.Web.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly IProductoRepository _repository;
    private readonly ProductoService _productoService;

    public CreateModel(
    IProductoRepository repository,
    ProductoService productoService)
    {
        _repository = repository;
        _productoService = productoService;
    }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public List<CategoriaOption> Categorias { get; private set; } = new();
    public List<MarcaOption> Marcas { get; private set; } = new();

    public void OnGet()
    {
        Categorias = _repository.ObtenerCategoriasParaFormulario();
        Marcas = _repository.ObtenerMarcasParaFormulario();
    }

    public IActionResult OnPost()
    {
        if (Input.FechaVencimiento.HasValue &&
            Input.FechaVencimiento.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                "Input.FechaVencimiento",
                "La fecha de vencimiento no puede estar en el pasado.");
        }

        if (!_repository.ExisteCategoriaActiva(Input.CategoriaId))
        {
            ModelState.AddModelError(
                "Input.CategoriaId",
                "La categoría seleccionada no está disponible.");
        }

        if (!_repository.ExisteMarcaActiva(Input.MarcaId))
        {
            ModelState.AddModelError(
                "Input.MarcaId",
                "La marca seleccionada no está disponible.");
        }

        Categorias = _repository.ObtenerCategoriasParaFormulario();
        Marcas = _repository.ObtenerMarcasParaFormulario();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        _productoService.RegistrarProductoConHistorico(Input);

        TempData["MensajeExito"] = "Producto registrado correctamente.";

        return RedirectToPage("Index");
    }

}

