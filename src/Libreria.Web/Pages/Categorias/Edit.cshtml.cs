using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;
using Libreria.Web.Pages.Categorias.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class EditModel : PageModel
{
    private readonly ICategoriaRepository _repository;
    private readonly CategoriaValidator _validator;

    public EditModel(
        ICategoriaRepository repository,
        CategoriaValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    [BindProperty]
    public int CategoriaId { get; set; }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var categoria = await _repository.ObtenerActivaPorIdAsync(id);
        if (categoria is null)
        {
            return NotFound();
        }

        CategoriaId = categoria.CategoriaId;
        Input = new CategoriaInput
        {
            Codigo = categoria.Codigo,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Ubicacion = categoria.Ubicacion
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _validator.Normalizar(Input);
        var errores = await _validator.ValidarAsync(Input, CategoriaId);
        AgregarErrores(errores);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var actualizada = await _repository.ActualizarAsync(new Categoria
        {
            CategoriaId = CategoriaId,
            Codigo = Input.Codigo,
            Nombre = Input.Nombre,
            Descripcion = Input.Descripcion,
            Ubicacion = Input.Ubicacion
        });

        if (!actualizada)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría actualizada correctamente.";
        return RedirectToPage("./Index");
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
