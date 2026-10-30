using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Productos;

public class EditModel : PageModel
{
    private readonly HttpClient _httpClient;

    public EditModel(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("VentasAPI");
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/productos/{id}");
        if (!response.IsSuccessStatusCode) return NotFound();

        var producto = await response.Content.ReadFromJsonAsync<ProductoDetalle>();
        if (producto == null) return NotFound();

        PublicId = id;
        Input = new ProductoInput
        {
            Codigo = producto.Codigo,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            CategoriaId = producto.CategoriaId,
            MarcaId = producto.MarcaId,
            PrecioCompra = producto.PrecioCompra,
            PrecioVenta = producto.PrecioVenta,
            StockMinimo = producto.StockMinimo
        };

        await CargarOpcionesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            PublicId = id;
            await CargarOpcionesAsync();
            return Page();
        }

        var response = await _httpClient.PutAsJsonAsync($"/api/productos/{id}", Input);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errores = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errores != null) AgregarErrores(errores);
            }
            PublicId = id;
            await CargarOpcionesAsync();
            return Page();
        }

        TempData["MensajeExito"] = "Producto actualizado correctamente.";
        return RedirectToPage("Details", new { id });
    }

    private async Task CargarOpcionesAsync()
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
