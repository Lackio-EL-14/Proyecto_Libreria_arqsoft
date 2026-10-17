namespace Libreria.Domain
{
    public class Venta
    {
        public int VentaId { get; set; }
        public Guid PublicId { get; set; }
        public int ClienteId { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Activa";
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaAnulacion { get; set; }
        public int UsuarioCreacionId { get; set; }
        public int? UsuarioAnulacionId { get; set; }
    }
}
