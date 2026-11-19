using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

/// <summary>Cambio de contraseña contra el proveedor de identidad (Keycloak simulado en memoria).</summary>
public sealed class ChangePasswordUseCaseTests
{
    private static readonly Guid Vendedor = ProveedorIdentidadEnMemoria.IdVendedorSemilla;
    private const string Actual = "Actual#2026";

    [Fact]
    public async Task DatosValidos_CambiaLaContrasenaEnElProveedor()
    {
        var (useCase, identidad) = await CrearAsync();

        var resultado = await useCase.ExecuteAsync(new ChangePasswordRequest(Actual, "Nueva#2026x", "Nueva#2026x"));

        Assert.True(resultado!.IsSuccess);
        Assert.True((await identidad.VerificarContrasenaAsync("vendedor", "Nueva#2026x")).IsSuccess);
        Assert.False((await identidad.TieneCambioDeContrasenaPendienteAsync(Vendedor)).Valor);
    }

    [Fact]
    public async Task SinUsuarioActual_DevuelveNull()
    {
        var (useCase, _) = await CrearAsync(new FakeCurrentUser());

        Assert.Null(await useCase.ExecuteAsync(new ChangePasswordRequest(Actual, "Nueva#2026x", "Nueva#2026x")));
    }

    [Theory]
    [InlineData("", "Nueva#2026x", "Nueva#2026x", "Ingrese su contraseña actual.")]
    [InlineData(Actual, "Nueva#2026x", "Otra#2026xx", "La confirmación no coincide con la nueva contraseña.")]
    [InlineData(Actual, Actual, Actual, "La nueva contraseña debe ser distinta de la actual.")]
    [InlineData(Actual, "Vendedor", "Vendedor", "La contraseña debe contener al menos un número.")]
    public async Task EntradaInvalida_DevuelveMensaje_SinTocarElProveedor(string actual, string nueva, string confirmacion, string mensaje)
    {
        var (useCase, identidad) = await CrearAsync();

        var resultado = await useCase.ExecuteAsync(new ChangePasswordRequest(actual, nueva, confirmacion));

        Assert.True(resultado!.IsError);
        Assert.Contains(mensaje, Assert.Single(resultado.Errors()));
        Assert.True((await identidad.VerificarContrasenaAsync("vendedor", Actual)).IsSuccess);
    }

    [Fact]
    public async Task NuevaIgualAlUsuario_SeRechaza()
    {
        var (useCase, _) = await CrearAsync(new FakeCurrentUser { UserId = Vendedor, Username = "Vendedor#2026" });

        var resultado = await useCase.ExecuteAsync(new ChangePasswordRequest(Actual, "Vendedor#2026", "Vendedor#2026"));

        Assert.Equal("La contraseña no puede ser igual al nombre de usuario.", Assert.Single(resultado!.Errors()));
    }

    [Fact]
    public async Task ContrasenaActualIncorrecta_SeRechaza()
    {
        var (useCase, identidad) = await CrearAsync();

        var resultado = await useCase.ExecuteAsync(new ChangePasswordRequest("Incorrecta#1", "Nueva#2026x", "Nueva#2026x"));

        Assert.Equal(MensajesIdentidad.ContrasenaActualIncorrecta, Assert.Single(resultado!.Errors()));
        Assert.True((await identidad.VerificarContrasenaAsync("vendedor", Actual)).IsSuccess);
    }

    private static async Task<(ChangePasswordUseCase, ProveedorIdentidadEnMemoria)> CrearAsync(FakeCurrentUser? usuario = null)
    {
        var identidad = new ProveedorIdentidadEnMemoria();
        await identidad.FijarContrasenaAsync(Vendedor, Actual, temporal: false);

        var useCase = new ChangePasswordUseCase(
            identidad,
            usuario ?? new FakeCurrentUser { UserId = Vendedor, Username = "vendedor" },
            new PasswordPolicyValidator());

        return (useCase, identidad);
    }
}
