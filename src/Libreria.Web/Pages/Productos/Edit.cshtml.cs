using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class EditModel : PageModel
{
    private readonly IProductoRepository _repository;
    private readonly CostoProductoService _costoProductoService;
    private readonly ProductoValidator _validator;

    public EditModel(
     IProductoRepository repository,
     CostoProductoService costoProductoService,
     ProductoValidator validator)
    {
        _repository = repository;
        _costoProductoService = costoProductoService;
        _validator = validator;
    }

    [BindProperty]
    public int ProductoId { get; set; }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public List<CategoriaOption> Categorias { get; private set; } = new();
    public List<MarcaOption> Marcas { get; private set; } = new();

    public IActionResult OnGet(int id)
    {
        var producto = _repository.ObtenerProductoPorId(id);

        if (producto is null)
        {
            return NotFound();
        }

        ProductoId = producto.ProductoId;
        Input = producto.CrearInputEdicion();
        CargarOpciones();

        return Page();
    }

    public IActionResult OnPost()
    {
        if (_repository.ObtenerProductoPorId(ProductoId) is null)
        {
            return NotFound();
        }

        NormalizarInput();
        ValidarInput();
        CargarOpciones();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!_repository.ActualizarProducto(ProductoId, Input))
        {
            return NotFound();
        }

        _costoProductoService.ActualizarCostoSiCambio(
            ProductoId,
            Input.CostoAdquisicion,
            "Edición manual");

        TempData["MensajeExito"] = "Producto actualizado correctamente.";

        return RedirectToPage("Details", new { id = ProductoId });
    }

    private void ValidarInput()
    {
        if (!_validator.EsFechaVencimientoValida(Input.FechaVencimiento))
        {
            ModelState.AddModelError(
                "Input.FechaVencimiento",
                "La fecha de vencimiento no puede estar en el pasado.");
        }

        if (Input.CategoriaId > 0 &&
            !_repository.ExisteCategoriaActiva(Input.CategoriaId))
        {
            ModelState.AddModelError(
                "Input.CategoriaId",
                "La categoría seleccionada no está disponible.");
        }

        if (Input.MarcaId > 0 &&
            !_repository.ExisteMarcaActiva(Input.MarcaId))
        {
            ModelState.AddModelError(
                "Input.MarcaId",
                "La marca seleccionada no está disponible.");
        }
    }

    private void CargarOpciones()
    {
        Categorias = _repository.ObtenerCategoriasParaFormulario();
        Marcas = _repository.ObtenerMarcasParaFormulario();
    }

    private void NormalizarInput()
    {
        Input.Nombre = _validator.NormalizarTexto(Input.Nombre ?? string.Empty);
        Input.DescripcionEspecifica =
            _validator.NormalizarTextoOpcional(Input.DescripcionEspecifica);
    }
}
