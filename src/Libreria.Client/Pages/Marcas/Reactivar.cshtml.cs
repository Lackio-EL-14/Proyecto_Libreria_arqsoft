using Libreria.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Marcas;

public class ReactivarModel : PageModel
{
    private readonly HttpClient _httpClient;
    public ReactivarModel(IHttpClientFactory factory) { _httpClient = factory.CreateClient("VentasAPI"); }

    public MarcaDtoBasico Marca { get; private set; } = null!;
    public Guid PublicId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        PublicId = id;
        var response = await _httpClient.GetAsync($"/api/marcas/{id}");
        if (!response.IsSuccessStatusCode) return NotFound();
        var mar = await response.Content.ReadFromJsonAsync<MarcaDtoBasico>();
        if (mar == null) return NotFound();
        Marca = mar;
        return Page();
    }
}

public class MarcaDtoBasico
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? PaisOrigen { get; set; }
    public string? SitioWeb { get; set; }
}
