namespace Libreria.Application.Domain
{
    public class Venta
    {
        public int VentaId { get; set; }
        public Guid PublicId { get; set; }
        public int ClienteId { get; set; }
        public string Estado { get; set; } = "Activa";
        public int UsuarioCreacionId { get; set; }
        public int? UsuarioAnulacionId { get; set; }
    }
}