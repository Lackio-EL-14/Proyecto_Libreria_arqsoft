using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Validators;
using Libreria.Application.Ports.Primary;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class EditModel : PageModel
{
    private readonly ICrudRepository<Marca> _repository;
    private readonly MarcaValidator _validator;
    private readonly IAuditoriaService _auditoriaService;

    public EditModel(
        CrudRepositoryFactory<Marca> factory,
        MarcaValidator validator,
        IAuditoriaService auditoriaService)
    {
        _repository = factory.CrearRepositorio();
        _validator = validator;
        _auditoriaService = auditoriaService;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public AuditoriaRegistro? Auditoria { get; private set; }

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

        Auditoria = await _auditoriaService.ObtenerAsync(
            marca.UsuarioCreacionId,
            null,
            marca.UsuarioModificacionId,
            null);

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