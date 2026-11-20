using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class PrimerInicioSesionTests
{
    [Fact]
    public async Task PrimerIngreso_ContrasenaTemporal_ExigeCambioYBloqueaAccesoNormal()
    {
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var user = new UsuarioIdentidad("nusuario", "nuevo@libreria.local", "Nuevo", "Usuario");

        var createResult = await identityProvider.CrearUsuarioAsync(user);
        var userId = createResult.Valor;
        await identityProvider.AsignarRolAsync(userId, "Vendedor");
        await identityProvider.FijarContrasenaAsync(userId, "Temporal#2026x", temporal: true);

        var pendingCheck = await identityProvider.TieneCambioDeContrasenaPendienteAsync(userId);
        Assert.True(pendingCheck.IsSuccess);
        Assert.True(pendingCheck.Valor);

        var verificationResult = await identityProvider.VerificarContrasenaAsync("nusuario", "Temporal#2026x");
        Assert.True(verificationResult.IsError);
        Assert.Contains(MensajesIdentidad.CambioPendiente, verificationResult.Errors());
    }

    [Fact]
    public async Task PrimerIngreso_TrasCambioDefinitivo_HabilitaAccesoYRemueveAccionPendiente()
    {
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var user = new UsuarioIdentidad("cusuario", "cambio@libreria.local", "Carlos", "Usuario");

        var createResult = await identityProvider.CrearUsuarioAsync(user);
        var userId = createResult.Valor;
        await identityProvider.AsignarRolAsync(userId, "Vendedor");
        await identityProvider.FijarContrasenaAsync(userId, "Temporal#2026x", temporal: true);

        var changeResult = await identityProvider.FijarContrasenaAsync(userId, "Definitiva#2026x", temporal: false);
        Assert.True(changeResult.IsSuccess);

        var pendingCheck = await identityProvider.TieneCambioDeContrasenaPendienteAsync(userId);
        Assert.True(pendingCheck.IsSuccess);
        Assert.False(pendingCheck.Valor);

        var verificationResult = await identityProvider.VerificarContrasenaAsync("cusuario", "Definitiva#2026x");
        Assert.True(verificationResult.IsSuccess);
    }

    [Fact]
    public async Task PrimerIngreso_ContrasenaDefinitivaInvalida_RechazaYConservaEstadoPendiente()
    {
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var user = new UsuarioIdentidad("iusuario", "invalido@libreria.local", "Invalido", "Usuario");

        var createResult = await identityProvider.CrearUsuarioAsync(user);
        var userId = createResult.Valor;
        await identityProvider.FijarContrasenaAsync(userId, "Temporal#2026x", temporal: true);

        var invalidChangeResult = await identityProvider.FijarContrasenaAsync(userId, "corta", temporal: false);
        Assert.True(invalidChangeResult.IsError);

        var pendingCheck = await identityProvider.TieneCambioDeContrasenaPendienteAsync(userId);
        Assert.True(pendingCheck.Valor);
    }
}
