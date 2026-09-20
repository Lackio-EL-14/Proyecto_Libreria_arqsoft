using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Pages.Productos;

public class EditModel : PageModel
{
    private readonly IProductoService _service;
    private readonly ICrudRepository<Producto> _repository;

    public EditModel(
     IProductoService service,
     CrudRepositoryFactory<Producto> factory)
    {
        _service = service;
        _repository = factory.CrearRepositorio();
    }
    public Guid PublicId { get; private set; }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var edicion = await _service.ObtenerEdicionAsync(id, _repository);

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
        var resultado = await _service.ActualizarAsync(id, Input, _repository);
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
        var edicion = await _service.ObtenerEdicionAsync(publicId, _repository);
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