using Libreria.Web.Pages.Marcas.Models;
using Libreria.Web.Pages.Marcas.Repositories;
using Libreria.Web.Pages.Marcas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class CreateModel : PageModel
{
    private readonly IRegistroMarcaRepository _repository;
    private readonly MarcaValidator _validator;

    public CreateModel(
        IRegistroMarcaRepository repository,
        MarcaValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public IActionResult OnPost()
    {
        _validator.Normalizar(Input);
        AgregarErrores(_validator.Validar(Input));
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _repository.Crear(new Marca
        {
            Nombre = Input.Nombre,
            Descripcion = Input.Descripcion,
            PaisOrigen = Input.PaisOrigen,
            SitioWeb = Input.SitioWeb
        });

        TempData["MensajeExito"] = "Marca registrada correctamente.";
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
