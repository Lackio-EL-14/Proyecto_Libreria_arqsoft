using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly HttpClient _httpClient;

    public IndexModel(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("VentasAPI");
    }

    public IReadOnlyList<ProductoListItem> Productos { get; private set; } = [];
    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoriaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MarcaId { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _httpClient.GetFromJsonAsync<List<CategoriaOption>>("/api/categorias/opciones") ?? new();
        Marcas = await _httpClient.GetFromJsonAsync<List<MarcaOption>>("/api/marcas/opciones") ?? new();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(Busqueda)) query.Add($"busqueda={Busqueda}");
        if (CategoriaId.HasValue) query.Add($"categoriaId={CategoriaId}");
        if (MarcaId.HasValue) query.Add($"marcaId={MarcaId}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
        Productos = await _httpClient.GetFromJsonAsync<List<ProductoListItem>>($"/api/productos{queryString}") ?? new();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var response = await _httpClient.PatchAsync($"/api/productos/{publicId}/estado?nuevoEstado=false", null);
        if (!response.IsSuccessStatusCode) return NotFound();

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

