namespace Libreria.Client.DTOs.Productos;

// DTOs propios del Front para el MS Productos.
// El Front no referencia el proyecto del servicio: solo conoce sus contratos JSON.

public record CategoriaDto(
    Guid PublicId,
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Ubicacion,
    bool Estado,
    Guid? UsuarioCreacionId,
    DateTime FechaCreacion,
    Guid? UsuarioModificacionId,
    DateTime? FechaModificacion,
    bool TieneProductosActivos);

public record MarcaDto(
    Guid PublicId,
    string Nombre,
    string? Descripcion,
    string PaisOrigen,
    string? SitioWeb,
    bool Estado,
    Guid? UsuarioCreacionId,
    DateTime FechaCreacion,
    Guid? UsuarioModificacionId,
    DateTime? FechaModificacion,
    bool TieneProductosActivos);

public class CategoriaInput
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
}

public class MarcaInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PaisOrigen { get; set; } = string.Empty;
    public string? SitioWeb { get; set; }
}
