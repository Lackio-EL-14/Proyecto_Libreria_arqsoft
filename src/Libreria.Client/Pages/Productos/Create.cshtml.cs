using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly HttpClient _httpClient;

    public CreateModel(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("VentasAPI");
    }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarFormulario();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarFormulario();
            return Page();
        }

        var response = await _httpClient.PostAsJsonAsync("/api/productos", Input);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errores = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errores != null) AgregarErrores(errores);
            }
            await CargarFormulario();
            return Page();
        }

        TempData["MensajeExito"] = "Producto registrado correctamente.";
        return RedirectToPage("Index");
    }

    private async Task CargarFormulario()
    {
        Categorias = await _httpClient.GetFromJsonAsync<List<CategoriaOption>>("/api/categorias/opciones") ?? new();
        Marcas = await _httpClient.GetFromJsonAsync<List<MarcaOption>>("/api/marcas/opciones") ?? new();
    }

    private void AgregarErrores(Dictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
