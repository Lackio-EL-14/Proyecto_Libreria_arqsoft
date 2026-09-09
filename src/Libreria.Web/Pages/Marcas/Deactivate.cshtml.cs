using Libreria.Web.Pages.Marcas.Models;
using Libreria.Web.Pages.Marcas.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class DeactivateModel : PageModel
{
    private readonly IMarcaRepository _repository;

    public DeactivateModel(IMarcaRepository repository)
    {
        _repository = repository;
    }

    public MarcaBajaView Marca { get; private set; } = new();

    [BindProperty]
    public int MarcaId { get; set; }

    public IActionResult OnGet(int id)
    {
        var marca = _repository.ObtenerActivaParaBaja(id);
        if (marca is null)
        {
            return NotFound();
        }

        Marca = marca;
        MarcaId = marca.MarcaId;
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!_repository.DarDeBaja(MarcaId))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}
