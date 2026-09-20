using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Catalogs;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Business.Validators;
using Libreria.Web.Pages.Categorias.Models; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

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
