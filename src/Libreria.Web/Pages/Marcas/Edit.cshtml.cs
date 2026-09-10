using Libreria.Web.Pages.Marcas.Models;
using Libreria.Web.Pages.Marcas.Repositories;
using Libreria.Web.Pages.Marcas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class EditModel : PageModel
{
    private readonly IMarcaRepository _repository;
    private readonly MarcaValidator _validator;

    public EditModel(
        IMarcaRepository repository,
        MarcaValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    [BindProperty]
    public int MarcaId { get; set; }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        var marca = _repository.ObtenerActivaPorId(id);
        if (marca is null)
        {
            return NotFound();
        }

        MarcaId = marca.MarcaId;
        Input = new MarcaInput
        {
            Nombre = marca.Nombre,
            Descripcion = marca.Descripcion,
            PaisOrigen = marca.PaisOrigen,
            SitioWeb = marca.SitioWeb
        };
        return Page();
    }

    public IActionResult OnPost()
    {
        _validator.Normalizar(Input);
        AgregarErrores(_validator.Validar(Input, MarcaId));
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var actualizada = _repository.Actualizar(new Marca
        {
            MarcaId = MarcaId,
            Nombre = Input.Nombre,
            Descripcion = Input.Descripcion,
            PaisOrigen = Input.PaisOrigen,
            SitioWeb = Input.SitioWeb
        });

        if (!actualizada)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca actualizada correctamente.";
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
