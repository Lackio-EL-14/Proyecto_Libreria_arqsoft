using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Libreria.Client.Pages.Ventas;

public class RegistrarModel : PageModel
{
    private readonly HttpClient _httpClient;

    public RegistrarModel(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("VentasAPI");
    }

    [BindProperty]
    public RegistrarVentaInput Input { get; set; } = new();

    [BindProperty]
    public ClienteInput NuevoCliente { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var response = await _httpClient.PostAsJsonAsync("/api/ventas/registrar", Input);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errores = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errores != null) AgregarErrores(errores);
            }
            return Page();
        }

        var ventaId = await response.Content.ReadFromJsonAsync<int>();

        TempData["MensajeExito"] = "Venta registrada exitosamente.";
        return RedirectToPage("Comprobante", new { id = ventaId });
    }

    private void AgregarErrores(Dictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}

// DTOs requeridos para la vista
public class RegistrarVentaInput
{
    public int ClienteId { get; set; }
    public decimal TotalVenta { get; set; }
    public List<DetalleVentaInput> Detalles { get; set; } = new();
}

public class DetalleVentaInput
{
    public Guid ProductoPublicId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class ClienteInput 
{
    public string CiNit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string TipoContacto { get; set; } = "Cliente";
}
