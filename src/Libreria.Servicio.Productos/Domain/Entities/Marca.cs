namespace Libreria.Servicio.Productos.Domain.Entities;

public sealed class Marca
{
    public Guid PublicId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PaisOrigen { get; set; } = string.Empty;
    public string? SitioWeb { get; set; }
    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? UsuarioCreacionId { get; set; }
    public Guid? UsuarioModificacionId { get; set; }
}