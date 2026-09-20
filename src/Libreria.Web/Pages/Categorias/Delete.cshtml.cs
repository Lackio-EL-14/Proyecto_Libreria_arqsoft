using System;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class DeleteModel : PageModel
{
    private readonly ICrudRepository<Categoria> _repository;

    public DeleteModel(CrudRepositoryFactory<Categoria> factory)
    {
        _repository = factory.CrearRepositorio();
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public Categoria? Categoria { get; private set; }
    public bool TieneProductos { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Categoria = await _repository.ObtenerPorPublicIdAsync(id, true);
        if (Categoria is null)
        {
            return NotFound();
        }

        PublicId = Categoria.PublicId;
        TieneProductos = await _repository.TieneRelacionesAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Dar de baja = cambiar estado a false
        if (!await _repository.CambiarEstadoAsync(PublicId, false))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}
