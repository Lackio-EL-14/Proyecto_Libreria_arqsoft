using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
namespace Libreria.Servicio.Ventas.Application.Models;

public record ResultadoOperacion(
    bool Exitoso,
    bool NoEncontrado,
    IReadOnlyDictionary<string, string> Errores)
{
    public static ResultadoOperacion Correcto() => new(true, false, new Dictionary<string, string>());
    public static ResultadoOperacion Invalido(IReadOnlyDictionary<string, string> errores) => new(false, false, errores);
    public static ResultadoOperacion NoExiste() => new(false, true, new Dictionary<string, string>());
}
