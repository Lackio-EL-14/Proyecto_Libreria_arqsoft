namespace Libreria.Servicio.Ventas.Domain
{
    public class Venta
    {
        public int VentaId { get; set; }
        public Guid PublicId { get; set; }
        public Guid ClientePublicId { get; set; }
        public string ClienteCiNit { get; set; } = string.Empty;
        public string ClienteRazonSocial { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Activa";
        public string? Observacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public Guid UsuarioCreacionId { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public Guid? UsuarioModificacionId { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string? MotivoAnulacion { get; set; }
        public Guid? UsuarioAnulacionId { get; set; }
        public string? VendedorIniciales { get; set; }
    }
}
