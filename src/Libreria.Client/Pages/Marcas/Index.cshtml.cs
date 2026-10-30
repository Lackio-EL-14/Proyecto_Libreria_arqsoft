using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Libreria.Application.Models;

namespace Libreria.Client.Pages.Marcas;

public class IndexModel : PageModel
{
    private readonly HttpClient _httpClient;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("VentasAPI");
    }

    public IReadOnlyList<MarcaDto> Marcas { get; private set; } = [];
    public IReadOnlySet<Guid> MarcasConProductosActivos { get; private set; } = new HashSet<Guid>();
    public string? NombreBusqueda { get; private set; }

    public async Task OnGetAsync(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();
        var url = string.IsNullOrWhiteSpace(NombreBusqueda) 
            ? "/api/marcas" 
            : $"/api/marcas?busqueda={NombreBusqueda}";

        Marcas = await _httpClient.GetFromJsonAsync<List<MarcaDto>>(url) ?? new List<MarcaDto>();

        var marcasConProductos = new HashSet<Guid>();
        foreach (var marca in Marcas)
        {
            var urlRelaciones = $"/api/marcas/{marca.PublicId}/tiene-relaciones";
            var tieneRelaciones = await _httpClient.GetFromJsonAsync<bool>(urlRelaciones);
            
            if (tieneRelaciones)
            {
                marcasConProductos.Add(marca.PublicId);
            }
        }
        MarcasConProductosActivos = marcasConProductos;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var url = $"/api/marcas/{publicId}/estado?nuevoEstado=false";
        var response = await _httpClient.PatchAsync(url, null);

        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

public record MarcaDto(Guid PublicId, string Nombre, string? Descripcion, string? PaisOrigen, string? SitioWeb, bool Activo);
