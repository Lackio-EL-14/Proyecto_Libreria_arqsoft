using System;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class ReactivarModel : PageModel
{
    private readonly ICrudRepository<Categoria> _repository;

    public ReactivarModel(CrudRepositoryFactory<Categoria> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public Categoria? Categoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Categoria = await _repository.ObtenerPorPublicIdAsync(id, false);
        if (Categoria is null)
        {
            return NotFound();
        }

        PublicId = Categoria.PublicId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _repository.CambiarEstadoAsync(PublicId, true))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría reactivada correctamente.";
        return RedirectToPage("./Index");
    }
}
