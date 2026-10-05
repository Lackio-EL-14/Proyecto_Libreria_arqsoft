using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using System.Collections.Generic;
using System.Threading.Tasks;

using Libreria.Application.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class CreateModel : PageModel
{
    private readonly ICrudRepository<Categoria> _repository;
    private readonly CategoriaValidator _validator;

    public CreateModel(
        CrudRepositoryFactory<Categoria> factory,
        CategoriaValidator validator)
    {
        _repository = factory.CrearRepositorio();
        _validator = validator;
    }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public IReadOnlyList<string> Ubicaciones => UbicacionesCategoria.Todas;

    public async Task<IActionResult> OnPostAsync()
    {
        _validator.Normalizar(Input);
        var errores = await _validator.ValidarCreacionAsync(Input);
        AgregarErrores(errores);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _repository.CrearAsync(new Categoria
        {
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
