using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
namespace Libreria.Application.Domain;

public class Marca
{
    public int MarcaId { get; set; }
    public Guid PublicId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PaisOrigen { get; set; } = string.Empty;
    public string? SitioWeb { get; set; }
    public bool Estado { get; set; }
    public int? UsuarioCreacionId { get; set; }
    public int? UsuarioModificacionId { get; set; }
    
}
