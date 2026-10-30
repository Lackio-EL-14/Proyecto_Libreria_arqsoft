using Libreria.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Categorias;

public class ReactivarModel : PageModel
{
    private readonly HttpClient _httpClient;
    public ReactivarModel(IHttpClientFactory factory) { _httpClient = factory.CreateClient("VentasAPI"); }

    public CategoriaDtoBasico Categoria { get; private set; } = null!;
    public Guid PublicId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        PublicId = id;
        var response = await _httpClient.GetAsync($"/api/categorias/{id}");
        if (!response.IsSuccessStatusCode) return NotFound();
        var cat = await response.Content.ReadFromJsonAsync<CategoriaDtoBasico>();
        if (cat == null) return NotFound();
        Categoria = cat;
        return Page();
    }
}

public class CategoriaDtoBasico
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Ubicacion { get; set; }
}
