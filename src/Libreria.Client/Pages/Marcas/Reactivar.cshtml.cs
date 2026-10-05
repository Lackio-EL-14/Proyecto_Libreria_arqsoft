using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class ReactivarModel : PageModel
{
    private readonly ICrudRepository<Marca> _repository;

    public ReactivarModel(CrudRepositoryFactory<Marca> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public Marca? Marca { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Marca = await _repository.ObtenerPorPublicIdAsync(
            id,
            estadoEsperado: false);

        if (Marca is null)
        {
            return NotFound();
        }

        PublicId = Marca.PublicId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actualizada = await _repository.CambiarEstadoAsync(
            PublicId,
            true);

        if (!actualizada)
        {
            return NotFound();
        }

        TempData["MensajeExito"] =
            "Marca reactivada correctamente.";

        return RedirectToPage("./Index");
    }
}