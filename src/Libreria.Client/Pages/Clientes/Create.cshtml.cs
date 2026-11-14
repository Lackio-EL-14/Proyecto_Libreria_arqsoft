using Libreria.Client.Clients.Clientes;
using Libreria.Client.DTOs.Clientes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Clientes;

public class CreateModel : PageModel
{
    private readonly IServicioClientesClient _clientes;

    public CreateModel(IServicioClientesClient clientes)
    {
        _clientes = clientes;
    }

    [BindProperty]
    public ClienteInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _clientes.RegistrarClienteAsync(Input);
        if (!resultado.Exitoso)
        {
            ModelState.AgregarErrores(resultado.Errores);
            return Page();
        }

        TempData["MensajeExito"] = "Cliente registrado correctamente.";
        return RedirectToPage("./Index");
    }
}
