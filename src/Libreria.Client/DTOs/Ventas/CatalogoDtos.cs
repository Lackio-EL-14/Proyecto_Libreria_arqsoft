namespace Libreria.Client.DTOs.Ventas;

// DTOs propios del Front para el Servicio de Ventas. El Front NO referencia el
// proyecto del servicio: solo conoce estos contratos JSON. Los nombres de las
// propiedades deben coincidir con las respuestas de Libreria.Servicio.Ventas.

public record CategoriaDto(
    Guid PublicId,
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Ubicacion,
    bool Estado,
    int? UsuarioCreacionId,
    DateTime FechaCreacion,
    int? UsuarioModificacionId,
    DateTime? FechaModificacion,
    bool TieneProductosActivos);

public record MarcaDto(
    Guid PublicId,
    string Nombre,
    string? Descripcion,
    string PaisOrigen,
    string? SitioWeb,
    bool Estado,
    int? UsuarioCreacionId,
    int? UsuarioModificacionId,
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
