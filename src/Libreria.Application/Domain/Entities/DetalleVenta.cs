namespace Libreria.Application.Domain
{
    public class DetalleVenta
    {
        public int DetalleVentaId { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitarioVenta { get; set; }
        public decimal CostoAdquisicionUnitario { get; set; }
        public decimal Importe { get; set; }
        public decimal Ganancia { get; set; }
    }
}