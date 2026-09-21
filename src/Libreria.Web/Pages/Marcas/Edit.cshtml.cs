using Libreria.Web.Business.Validators;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Pages.Marcas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class EditModel : PageModel
{
    private readonly ICrudRepository<Marca> _repository;
    private readonly MarcaValidator _validator;

    public EditModel(
        CrudRepositoryFactory<Marca> factory,
        MarcaValidator validator)
    {
        _repository = factory.CrearRepositorio();
        _validator = validator;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var marca = await _repository.ObtenerPorPublicIdAsync(
            id,
            estadoEsperado: true);

        if (marca is null)
        {
            return NotFound();
        }

        PublicId = marca.PublicId;

        Input = new MarcaInput
        {
            Nombre = marca.Nombre,
            Descripcion = marca.Descripcion,
            PaisOrigen = marca.PaisOrigen,
            SitioWeb = marca.SitioWeb
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _validator.Normalizar(Input);

        var errores = await _validator.ValidarAsync(
            Input,
            PublicId);

        AgregarErrores(errores);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var actualizada = await _repository.ActualizarAsync(
            new Marca
            {
                PublicId = PublicId,
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                PaisOrigen = Input.PaisOrigen,
                SitioWeb = Input.SitioWeb
            });

        if (!actualizada)
        {
            return NotFound();
        }

        TempData["MensajeExito"] =
            "Marca actualizada correctamente.";

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