namespace Libreria.Servicio.Productos.Domain.Entities;

public sealed class ReferenciaCatalogo
{
    public Guid PublicId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}