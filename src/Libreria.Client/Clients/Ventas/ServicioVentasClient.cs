using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Ventas;
using static Libreria.Client.Clients.Common.ConsultaHttp;

namespace Libreria.Client.Clients.Ventas;

public sealed class ServicioVentasClient : ApiClientBase, IServicioVentasClient
{
    public ServicioVentasClient(HttpClient httpClient)
        : base(httpClient, "Ventas")
    {
    }

    public async Task<IReadOnlyList<VentaListItemDto>> ObtenerVentasAsync(
        string? estado,
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        OrdenVentasDto orden,
        bool descendente,
        CancellationToken ct = default)
    {
        var url = "api/ventas" + Armar(
            ("estado", estado),
            ("fechaDesde", fechaDesde?.ToString("yyyy-MM-dd")),
            ("fechaHasta", fechaHasta?.ToString("yyyy-MM-dd")),
            ("orden", orden.ToString()),
            ("descendente", Booleano(descendente)));

        return await LeerAsync<List<VentaListItemDto>>(url, ct) ?? [];
    }

    public Task<VentaDetalleDto?> ObtenerDetalleVentaAsync(Guid publicId, CancellationToken ct = default) =>
        LeerOpcionalAsync<VentaDetalleDto>($"api/ventas/{publicId}", ct);

    public Task<ResultadoApi> CorregirVentaAsync(
        Guid publicId,
        CorregirVentaDto input,
        CancellationToken ct = default) =>
        EnviarPutAsync($"api/ventas/{publicId}", input, ct);

    public Task<ResultadoApi> AnularVentaAsync(Guid publicId, string motivo, CancellationToken ct = default) =>
        EnviarPostAsync($"api/ventas/{publicId}/anular", new { motivo }, ct);

    public Task<ComprobanteVentaDto?> ObtenerComprobanteAsync(Guid publicId, CancellationToken ct = default) =>
        LeerOpcionalAsync<ComprobanteVentaDto>($"api/ventas/{publicId}/comprobante", ct);
}
