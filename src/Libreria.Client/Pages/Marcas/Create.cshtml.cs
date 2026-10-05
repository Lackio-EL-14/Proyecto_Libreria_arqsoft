using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Validators;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class CreateModel : PageModel
{
    private readonly ICrudRepository<Marca> _repository;
    private readonly MarcaValidator _validator;

    public CreateModel(
        CrudRepositoryFactory<Marca> factory,
        MarcaValidator validator)
    {
        _repository = factory.CrearRepositorio();
        _validator = validator;
    }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        _validator.Normalizar(Input);

        var errores = await _validator.ValidarAsync(Input);
        AgregarErrores(errores);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _repository.CrearAsync(new Marca
        {
            Nombre = Input.Nombre,
            Descripcion = Input.Descripcion,
            PaisOrigen = Input.PaisOrigen,
            SitioWeb = Input.SitioWeb
        });

        TempData["MensajeExito"] =
            "Marca registrada correctamente.";

        return RedirectToPage("./Index");
    }

    private void AgregarErrores(
        IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(
                error.Key,
                error.Value);
        }
    }
}