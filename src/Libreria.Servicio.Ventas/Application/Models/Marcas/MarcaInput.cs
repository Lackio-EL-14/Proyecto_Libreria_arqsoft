using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
namespace Libreria.Servicio.Ventas.Application.Models;

public class MarcaInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PaisOrigen { get; set; } = string.Empty;
    public string? SitioWeb { get; set; }
}
