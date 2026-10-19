using Libreria.Application.Models;
using Libreria.Application.Services;

namespace Libreria.Application.Tests;

public class UsuarioAltaValidacionesTests
{
    [Theory]
    [InlineData("", "Peña", "ana@example.test", 2, "El nombre es obligatorio.")]
    [InlineData("Ana", "", "ana@example.test", 2, "El apellido es obligatorio.")]
    [InlineData("Ana1", "Peña", "ana@example.test", 2, "El nombre solo permite letras")]
    [InlineData("Ana", "Peña!", "ana@example.test", 2, "El apellido solo permite letras")]
    [InlineData("Ana", "Peña", "", 2, "El correo es obligatorio.")]
    [InlineData("Ana", "Peña", "incorrecto", 2, "El correo no tiene un formato válido.")]
    [InlineData("Ana", "Peña", "Ana <ana@example.test>", 2, "El correo no tiene un formato válido.")]
    [InlineData("Ana", "Peña", "ana@example.test", 0, "Debe seleccionar un rol válido.")]
    public async Task CrearPendienteAsync_DatosInvalidos_RechazaAntesDeAccederALaBase(
        string nombre, string apellido, string correo, int rolId, string mensaje)
    {
        // Ninguna dependencia debe utilizarse cuando falla la validación de entrada.
        var servicio = new UsuarioService(null!, null!, null!, null!, null!, null!, null!, null!);
        var error = await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearPendienteAsync(
            new UsuarioAltaInput { Nombre = nombre, Apellido = apellido, Correo = correo, RolId = rolId }));
        Assert.StartsWith(mensaje, error.Message);
    }
}
