using System;

namespace Libreria.Web.Domain.Entities
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public Guid PublicId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? DescripcionEspecifica { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int Stock { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal CostoAdquisicionActual { get; set; }
        public int CategoriaId { get; set; }
        public int MarcaId { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }
}