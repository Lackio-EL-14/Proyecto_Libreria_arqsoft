using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class ReactivarModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public ReactivarModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public CategoriaDto? Categoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Categoria = await _ventas.ObtenerCategoriaAsync(id, false);
        if (Categoria is null)
        {
            return NotFound();
        }

        PublicId = Categoria.PublicId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _ventas.CambiarEstadoCategoriaAsync(PublicId, true);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría reactivada correctamente.";
        return RedirectToPage("./Index");
    }
}
