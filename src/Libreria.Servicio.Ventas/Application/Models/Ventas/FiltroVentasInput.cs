namespace Libreria.Servicio.Ventas.Application.Models;

public class FiltroVentasInput
{
    public string? Estado { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public OrdenVentas Orden { get; set; } = OrdenVentas.Fecha;
    public bool Descendente { get; set; } = true;
}

public enum OrdenVentas
{
    Fecha,
    Total
}
