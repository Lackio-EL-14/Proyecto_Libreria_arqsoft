using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Pages.Marcas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class DeactivateModel : PageModel
{
    private readonly ICrudRepository<Marca> _repository;

    public DeactivateModel(CrudRepositoryFactory<Marca> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    public MarcaBajaView Marca { get; private set; } = new();

    [BindProperty]
    public Guid PublicId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var marca = await _repository.ObtenerPorPublicIdAsync(
            id,
            estadoEsperado: true);

        if (marca is null)
        {
            return NotFound();
        }

        var tieneRelaciones =
            await _repository.TieneRelacionesAsync(marca.PublicId);

        Marca = new MarcaBajaView
        {
            MarcaId = marca.MarcaId,
            Nombre = marca.Nombre,
            Descripcion = marca.Descripcion,
            PaisOrigen = marca.PaisOrigen,
            SitioWeb = marca.SitioWeb,
            ProductosActivos = tieneRelaciones ? 1 : 0
        };

        PublicId = marca.PublicId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actualizada = await _repository.CambiarEstadoAsync(
            PublicId,
            false);

        if (!actualizada)
        {
            return NotFound();
        }

        TempData["MensajeExito"] =
            "Marca dada de baja correctamente.";

        return RedirectToPage("./Index");
    }
}