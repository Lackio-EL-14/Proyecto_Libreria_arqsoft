namespace Libreria.Servicio.Productos.Domain.Entities;

public sealed class Categoria
{
    public Guid PublicId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? UsuarioCreacionId { get; set; }
    public Guid? UsuarioModificacionId { get; set; }
}