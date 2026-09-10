using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;
using Libreria.Web.Pages.Categorias.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class CreateModel : PageModel
{
    private readonly ICategoriaRepository _repository;
    private readonly CategoriaValidator _validator;

    public CreateModel(
        ICategoriaRepository repository,
        CategoriaValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        _validator.Normalizar(Input);
        var errores = await _validator.ValidarAsync(Input);
        AgregarErrores(errores);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _repository.CrearAsync(new Categoria
        {
            Codigo = Input.Codigo,
            Nombre = Input.Nombre,
            Descripcion = Input.Descripcion,
            Ubicacion = Input.Ubicacion
        });

        TempData["MensajeExito"] = "Categoría registrada exitosamente.";
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
