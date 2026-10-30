using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Categorias;

public class IndexModel : PageModel
{
    private readonly HttpClient _httpClient;

    // Inyectamos el cliente HTTP que configuraste apuntando a http://localhost:5101/
    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("VentasAPI");
    }

    // Usamos el DTO en lugar de la entidad de dominio original
    public IReadOnlyList<CategoriaDto> Categorias { get; private set; } = [];
    public IReadOnlySet<Guid> CategoriasConProductosActivos { get; private set; } = new HashSet<Guid>();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        // 1. Obtener la lista filtrada desde la API
        var url = string.IsNullOrWhiteSpace(Busqueda) 
            ? "/api/categorias" 
            : $"/api/categorias?busqueda={Busqueda}";
        
        Categorias = await _httpClient.GetFromJsonAsync<List<CategoriaDto>>(url) ?? new List<CategoriaDto>();

        // 2. Verificar relaciones para cada categoría
        var categoriasConProductosActivos = new HashSet<Guid>();
        foreach (var categoria in Categorias)
        {
            var urlRelaciones = $"/api/categorias/{categoria.PublicId}/tiene-relaciones";
            var tieneRelaciones = await _httpClient.GetFromJsonAsync<bool>(urlRelaciones);
            
            if (tieneRelaciones)
            {
                categoriasConProductosActivos.Add(categoria.PublicId);
            }
        }

        CategoriasConProductosActivos = categoriasConProductosActivos;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        // Llamar al endpoint para cambiar el estado a inactivo
        var url = $"/api/categorias/{publicId}/estado?nuevoEstado=false";
        var response = await _httpClient.PatchAsync(url, null);

        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

// DTO local para la vista (puedes moverlo a una carpeta Models compartida luego)
public record CategoriaDto(Guid PublicId, string Nombre, string? Descripcion, bool Activo);
