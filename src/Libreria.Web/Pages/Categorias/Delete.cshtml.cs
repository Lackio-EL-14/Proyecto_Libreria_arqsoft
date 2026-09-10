using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class DeleteModel : PageModel
{
    private readonly ICategoriaRepository _repository;

    public DeleteModel(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public int CategoriaId { get; set; }

    public Categoria? Categoria { get; private set; }
    public bool TieneProductos { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Categoria = await _repository.ObtenerActivaPorIdAsync(id);
        if (Categoria is null)
        {
            return NotFound();
        }

        CategoriaId = Categoria.CategoriaId;
        TieneProductos = await _repository.TieneProductosActivosAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _repository.DarDeBajaAsync(CategoriaId))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}
