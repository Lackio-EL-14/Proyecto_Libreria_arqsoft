using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class ReactivarModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public ReactivarModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public MarcaDto? Marca { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Marca = await _ventas.ObtenerMarcaAsync(id, false);
        if (Marca is null)
        {
            return NotFound();
        }

        PublicId = Marca.PublicId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _ventas.CambiarEstadoMarcaAsync(PublicId, true);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca reactivada correctamente.";
        return RedirectToPage("./Index");
    }
}
