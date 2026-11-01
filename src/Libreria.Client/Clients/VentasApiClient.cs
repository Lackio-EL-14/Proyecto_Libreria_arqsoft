using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Libreria.Client.DTOs.Ventas;

namespace Libreria.Client.Clients;

public class VentasApiClient : IVentasApiClient
{
    private const string MensajeSinConexion =
        "No se pudo conectar con el servicio de Ventas. Verifique que esté en ejecución.";

    private readonly HttpClient _http;

    public VentasApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------- Categorías ----------

    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(string? busqueda, CancellationToken ct = default)
    {
        var url = "api/categorias" + Consulta(("busqueda", busqueda));
        return await LeerAsync<List<CategoriaDto>>(url, ct) ?? new List<CategoriaDto>();
    }

    public Task<CategoriaDto?> ObtenerCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default)
    {
        return LeerOpcionalAsync<CategoriaDto>(
            $"api/categorias/{publicId}?estadoEsperado={Booleano(activa)}", ct);
    }

    public async Task<IReadOnlyList<string>> ObtenerUbicacionesCategoriaAsync(CancellationToken ct = default)
    {
        return await LeerAsync<List<string>>("api/categorias/ubicaciones", ct) ?? new List<string>();
    }

    public Task<ResultadoApi> CrearCategoriaAsync(CategoriaInput input, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PostAsJsonAsync("api/categorias", input, ct), ct);
    }

    public Task<ResultadoApi> ActualizarCategoriaAsync(Guid publicId, CategoriaInput input, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PutAsJsonAsync($"api/categorias/{publicId}", input, ct), ct);
    }

    public Task<ResultadoApi> CambiarEstadoCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default)
    {
        return EnviarAsync(
            () => _http.PatchAsync($"api/categorias/{publicId}/estado?nuevoEstado={Booleano(activa)}", null, ct), ct);
    }

    // ---------- Marcas ----------

    public async Task<IReadOnlyList<MarcaDto>> ObtenerMarcasAsync(string? nombre, CancellationToken ct = default)
    {
        var url = "api/marcas" + Consulta(("nombre", nombre));
        return await LeerAsync<List<MarcaDto>>(url, ct) ?? new List<MarcaDto>();
    }

    public Task<MarcaDto?> ObtenerMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default)
    {
        return LeerOpcionalAsync<MarcaDto>(
            $"api/marcas/{publicId}?estadoEsperado={Booleano(activa)}", ct);
    }

    public async Task<IReadOnlyList<string>> ObtenerPaisesMarcaAsync(CancellationToken ct = default)
    {
        return await LeerAsync<List<string>>("api/marcas/paises", ct) ?? new List<string>();
    }

    public Task<ResultadoApi> CrearMarcaAsync(MarcaInput input, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PostAsJsonAsync("api/marcas", input, ct), ct);
    }

    public Task<ResultadoApi> ActualizarMarcaAsync(Guid publicId, MarcaInput input, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PutAsJsonAsync($"api/marcas/{publicId}", input, ct), ct);
    }

    public Task<ResultadoApi> CambiarEstadoMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default)
    {
        return EnviarAsync(
            () => _http.PatchAsync($"api/marcas/{publicId}/estado?nuevoEstado={Booleano(activa)}", null, ct), ct);
    }

    // ---------- Productos ----------

    public async Task<ProductoListadoDto> ObtenerProductosAsync(
        string? busqueda, int? categoriaId, int? marcaId, CancellationToken ct = default)
    {
        var url = "api/productos" + Consulta(
            ("busqueda", busqueda),
            ("categoriaId", categoriaId?.ToString()),
            ("marcaId", marcaId?.ToString()));

        return await LeerAsync<ProductoListadoDto>(url, ct)
            ?? new ProductoListadoDto(
                new List<ProductoListItemDto>(),
                new List<CategoriaOptionDto>(),
                new List<MarcaOptionDto>());
    }

    public async Task<ProductoFormularioDto> ObtenerFormularioProductoAsync(CancellationToken ct = default)
    {
        return await LeerAsync<ProductoFormularioDto>("api/productos/formulario", ct)
            ?? new ProductoFormularioDto(
                new List<CategoriaOptionDto>(),
                new List<MarcaOptionDto>());
    }

    public Task<ProductoDetalleDto?> ObtenerProductoAsync(Guid publicId, CancellationToken ct = default)
    {
        return LeerOpcionalAsync<ProductoDetalleDto>($"api/productos/{publicId}", ct);
    }

    public Task<ProductoEdicionDto?> ObtenerProductoParaEdicionAsync(Guid publicId, CancellationToken ct = default)
    {
        return LeerOpcionalAsync<ProductoEdicionDto>($"api/productos/{publicId}/edicion", ct);
    }

    public Task<ResultadoApi> RegistrarProductoAsync(ProductoInput input, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PostAsJsonAsync("api/productos", input, ct), ct);
    }

    public Task<ResultadoApi> ActualizarProductoAsync(Guid publicId, ProductoInput input, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PutAsJsonAsync($"api/productos/{publicId}", input, ct), ct);
    }

    public Task<ResultadoApi> DarDeBajaProductoAsync(Guid publicId, CancellationToken ct = default)
    {
        return EnviarAsync(() => _http.PatchAsync($"api/productos/{publicId}/baja", null, ct), ct);
    }

    // ---------- Histórico ----------

    public Task<HistoricoProductoDto?> ObtenerHistoricoAsync(Guid productoPublicId, CancellationToken ct = default)
    {
        return LeerOpcionalAsync<HistoricoProductoDto>($"api/historico/{productoPublicId}", ct);
    }

    // ---------- Ventas ----------

    public async Task<IReadOnlyList<VentaListItemDto>> ObtenerVentasAsync(
        string? estado, DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken ct = default)
    {
        var url = "api/ventas" + Consulta(
            ("estado", estado),
            ("fechaDesde", fechaDesde?.ToString("yyyy-MM-dd")),
            ("fechaHasta", fechaHasta?.ToString("yyyy-MM-dd")));

        return await LeerAsync<List<VentaListItemDto>>(url, ct) ?? new List<VentaListItemDto>();
    }

    public Task<VentaDetalleDto?> ObtenerDetalleVentaAsync(
        Guid publicId, bool esAdministrador, CancellationToken ct = default)
    {
        return LeerOpcionalAsync<VentaDetalleDto>(
            $"api/ventas/{publicId}?esAdministrador={Booleano(esAdministrador)}", ct);
    }

    public Task<ResultadoApi> AnularVentaAsync(Guid publicId, string motivo, CancellationToken ct = default)
    {
        return EnviarAsync(
            () => _http.PostAsJsonAsync($"api/ventas/{publicId}/anular", new { motivo }, ct), ct);
    }

    // ---------- Ayudantes ----------

    private async Task<T?> LeerAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var respuesta = await _http.GetAsync(url, ct);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<T?> LeerOpcionalAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var respuesta = await _http.GetAsync(url, ct);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private static async Task<ResultadoApi> EnviarAsync(
        Func<Task<HttpResponseMessage>> peticion, CancellationToken ct)
    {
        try
        {
            using var respuesta = await peticion();
            return await ConvertirAsync(respuesta, ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoApi.Invalido(MensajeSinConexion);
        }
    }

    private static async Task<ResultadoApi> ConvertirAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return ResultadoApi.Correcto();
        }

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            var mensajeNotFound = await ExtraerMensajeAsync(respuesta, ct);
            return !string.IsNullOrWhiteSpace(mensajeNotFound)
                ? ResultadoApi.Invalido(mensajeNotFound)
                : ResultadoApi.NoExiste();
        }

        if (respuesta.StatusCode == HttpStatusCode.Conflict)
        {
            var mensajeConflict = await ExtraerMensajeAsync(respuesta, ct);
            return ResultadoApi.Invalido(
                !string.IsNullOrWhiteSpace(mensajeConflict) ? mensajeConflict : "La operación entra en conflicto con el estado actual.");
        }

        if (respuesta.StatusCode == HttpStatusCode.BadRequest)
        {
            var errores = await LeerErroresAsync(respuesta, ct);
            if (errores.Count > 0)
            {
                return ResultadoApi.Invalido(errores);
            }

            var mensajeBadRequest = await ExtraerMensajeAsync(respuesta, ct);
            if (!string.IsNullOrWhiteSpace(mensajeBadRequest))
            {
                return ResultadoApi.Invalido(mensajeBadRequest);
            }
        }

        return ResultadoApi.Invalido(
            $"El servicio de Ventas no pudo completar la operación (HTTP {(int)respuesta.StatusCode}).");
    }

    private static async Task<string?> ExtraerMensajeAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(
                await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("mensaje", out var propMensaje))
            {
                return propMensaje.GetString();
            }
            if (doc.RootElement.TryGetProperty("detail", out var propDetail))
            {
                return propDetail.GetString();
            }
        }
        catch
        {
        }

        return null;
    }

    private static async Task<IReadOnlyDictionary<string, string>> LeerErroresAsync(
        HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            var errores = await respuesta.Content
                .ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);

            return errores ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static string Consulta(params (string Clave, string? Valor)[] parametros)
    {
        var partes = parametros
            .Where(p => !string.IsNullOrWhiteSpace(p.Valor))
            .Select(p => $"{p.Clave}={Uri.EscapeDataString(p.Valor!.Trim())}")
            .ToList();

        return partes.Count == 0 ? string.Empty : "?" + string.Join('&', partes);
    }

    private static string Booleano(bool valor) => valor ? "true" : "false";
}
