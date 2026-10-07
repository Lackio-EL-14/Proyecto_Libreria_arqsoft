namespace Libreria.Application.Models;

public class DetalleVentaInput
{
    public Guid ProductoPublicId { get; set; }

    public int Cantidad { get; set; }
}