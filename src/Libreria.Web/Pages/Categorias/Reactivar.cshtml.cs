using Libreria.Web.Pages.Categorias.Models;
using Libreria.Web.Pages.Categorias.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class ReactivarModel : PageModel
{
    private readonly ICategoriaRepository _repository;

    public ReactivarModel(ICategoriaRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public int CategoriaId { get; set; }

    public Categoria? Categoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Categoria = await _repository.ObtenerInactivaPorIdAsync(id);
        if (Categoria is null)
        {
            return NotFound();
        }

        CategoriaId = Categoria.CategoriaId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await _repository.ReactivarAsync(CategoriaId))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría reactivada correctamente.";
        return RedirectToPage("./Index");
    }
}
