using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Domain;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;

namespace Libreria.Servicio.Usuarios.Tests.Identidad;

public sealed class ProveedorIdentidadEnMemoriaTests
{
    private static readonly UsuarioIdentidad Juan = new("jperezr", "juan@libreria.local", "Juan José", "Pérez Rojas");

    [Fact]
    public void TraeLosUsuariosSemillaConSusGuidFijos()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();

        Assert.Equal("admin", proveedor.Usuarios[ProveedorIdentidadEnMemoria.IdAdminSemilla].Datos.NombreUsuario);
        Assert.Contains("Administrador", proveedor.Usuarios[ProveedorIdentidadEnMemoria.IdAdminSemilla].Roles);
        Assert.Contains("Vendedor", proveedor.Usuarios[ProveedorIdentidadEnMemoria.IdVendedorSemilla].Roles);
    }

    [Fact]
    public async Task CrearUsuario_Duplicado_DevuelveConflicto()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();
        Assert.True((await proveedor.CrearUsuarioAsync(Juan)).IsSuccess);

        var duplicado = await proveedor.CrearUsuarioAsync(Juan with { Correo = "otro@libreria.local" });

        Assert.True(duplicado.IsError);
        Assert.Equal(TipoError.Conflicto, duplicado.Tipo);
    }

    [Fact]
    public async Task AsignarRol_Inexistente_DevuelveError()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();
        var id = (await proveedor.CrearUsuarioAsync(Juan)).Valor;

        Assert.True((await proveedor.AsignarRolAsync(id, "Vendedor")).IsSuccess);
        Assert.True((await proveedor.AsignarRolAsync(id, "Gerente")).IsError);
    }

    [Theory]
    [InlineData("Corta1!", "al menos 8 caracteres")]
    [InlineData("sinmayuscula1!", "mayúscula")]
    [InlineData("SINMINUSCULA1!", "minúscula")]
    [InlineData("SinNumeros!!", "número")]
    [InlineData("SinEspecial123", "carácter especial")]
    public async Task FijarContrasena_QueIncumpleLaPolitica_DevuelveElMensajeDeLaRegla(string contrasena, string fragmento)
    {
        var proveedor = new ProveedorIdentidadEnMemoria();

        var resultado = await proveedor.FijarContrasenaAsync(ProveedorIdentidadEnMemoria.IdAdminSemilla, contrasena, temporal: false);

        Assert.True(resultado.IsError);
        Assert.Contains(fragmento, Assert.Single(resultado.Errors()));
    }

    [Fact]
    public async Task FijarContrasena_IgualAlUsuario_SeRechaza()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();
        var id = (await proveedor.CrearUsuarioAsync(Juan with { NombreUsuario = "Jperez.2026" })).Valor;

        var resultado = await proveedor.FijarContrasenaAsync(id, "Jperez.2026", temporal: false);

        Assert.Equal("La contraseña no puede ser igual al nombre de usuario.", Assert.Single(resultado.Errors()));
    }

    [Fact]
    public async Task ContrasenaTemporal_DejaElCambioPendiente_YNoPermiteVerificar()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();
        var id = (await proveedor.CrearUsuarioAsync(Juan)).Valor;

        await proveedor.FijarContrasenaAsync(id, "Temporal#2026", temporal: true);

        Assert.True((await proveedor.TieneCambioDeContrasenaPendienteAsync(id)).Valor);
        var verificacion = await proveedor.VerificarContrasenaAsync("jperezr", "Temporal#2026");
        Assert.Equal(MensajesIdentidad.CambioPendiente, Assert.Single(verificacion.Errors()));

        await proveedor.FijarContrasenaAsync(id, "Definitiva#2026", temporal: false);

        Assert.False((await proveedor.TieneCambioDeContrasenaPendienteAsync(id)).Valor);
        Assert.True((await proveedor.VerificarContrasenaAsync("jperezr", "Definitiva#2026")).IsSuccess);
        Assert.Equal(MensajesIdentidad.ContrasenaActualIncorrecta,
            Assert.Single((await proveedor.VerificarContrasenaAsync("jperezr", "Otra#2026xx")).Errors()));
    }

    [Fact]
    public async Task Deshabilitar_ImpideVerificar_YHabilitarLoDevuelve()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();
        var id = ProveedorIdentidadEnMemoria.IdVendedorSemilla;
        await proveedor.FijarContrasenaAsync(id, "Vendedor#2026", temporal: false);

        await proveedor.DeshabilitarAsync(id);
        Assert.Equal(MensajesIdentidad.CuentaDeshabilitada,
            Assert.Single((await proveedor.VerificarContrasenaAsync("vendedor", "Vendedor#2026")).Errors()));

        await proveedor.HabilitarAsync(id);
        Assert.True((await proveedor.VerificarContrasenaAsync("vendedor", "Vendedor#2026")).IsSuccess);
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveNoEncontrado_YEliminarEsIdempotente()
    {
        var proveedor = new ProveedorIdentidadEnMemoria();
        var id = Guid.NewGuid();

        Assert.Equal(TipoError.NoEncontrado, (await proveedor.DeshabilitarAsync(id)).Tipo);
        Assert.Equal(TipoError.NoEncontrado, (await proveedor.TieneCambioDeContrasenaPendienteAsync(id)).Tipo);
        Assert.True((await proveedor.EliminarUsuarioAsync(id)).IsSuccess);
    }

    [Fact]
    public async Task FallarSiguienteLlamada_SimulaKeycloakCaido_UnaSolaVez()
    {
        var proveedor = new ProveedorIdentidadEnMemoria { FallarSiguienteLlamada = new HttpRequestException("caído") };

        await Assert.ThrowsAsync<HttpRequestException>(() => proveedor.CrearUsuarioAsync(Juan));
        Assert.True((await proveedor.CrearUsuarioAsync(Juan)).IsSuccess);
    }
}
