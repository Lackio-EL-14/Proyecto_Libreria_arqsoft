using System.Data;
using Libreria.Application.Models;
using Libreria.Application.Services;
using Libreria.Domain;

namespace Libreria.Application.Tests;

public class UsuarioSeguridadServiceTests
{
    internal static UsuarioBajaDatos Usuario(int id, string rol = RolesSistema.Administrador,
        string estado = EstadosUsuario.Activo) => new(id, Guid.NewGuid(), estado, rol, true);

    [Theory]
    [InlineData(EstadosUsuario.Activo)]
    [InlineData(EstadosUsuario.Pendiente)]
    public async Task DarDeBajaAsync_UsuarioAjeno_CambiaEstadoConActorYTransaccion(string estado)
    {
        var actor = Usuario(1);
        var destino = Usuario(2, RolesSistema.Vendedor, estado);
        var repo = new SeguridadRepositoryFake(actor, destino);
        var conexion = new ConexionBajaFake();
        var servicio = new UsuarioSeguridadService(repo, conexion, new UsuarioActualFake(1));

        Assert.True((await servicio.DarDeBajaAsync(destino.PublicId)).Exitoso);
        Assert.Equal(2, repo.BajaId);
        Assert.Equal(1, repo.ActorId);
        Assert.Same(repo.TransaccionLectura, repo.TransaccionEscritura);
        Assert.True(conexion.Transaccion!.Confirmada);
        Assert.Equal(IsolationLevel.Serializable, conexion.Transaccion.IsolationLevel);
    }

    [Fact]
    public async Task DarDeBajaAsync_UltimoAdministrador_RechazaSinModificar()
    {
        var actor = Usuario(1);
        var repo = new SeguridadRepositoryFake(actor);
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));

        var resultado = await servicio.DarDeBajaAsync(actor.PublicId);

        Assert.False(resultado.Exitoso);
        Assert.Contains("último administrador activo", Assert.Single(resultado.Errores).Value);
        Assert.Null(repo.BajaId);
    }

    [Fact]
    public async Task DarDeBajaAsync_PropioAdministradorConOtroActivo_RechazaSinModificar()
    {
        var actor = Usuario(1);
        var repo = new SeguridadRepositoryFake(actor, Usuario(2));
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));

        var resultado = await servicio.DarDeBajaAsync(actor.PublicId);

        Assert.Contains("sí mismo", Assert.Single(resultado.Errores).Value);
        Assert.Null(repo.BajaId);
    }

    [Theory]
    [InlineData(EstadosUsuario.Inactivo)]
    [InlineData(EstadosUsuario.Pendiente)]
    public async Task DarDeBajaAsync_ActorDesactivadoMientrasEsperaba_Rechaza(string estado)
    {
        var destino = Usuario(2);
        var repo = new SeguridadRepositoryFake(Usuario(1, estado: estado), destino);
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));

        Assert.False((await servicio.DarDeBajaAsync(destino.PublicId)).Exitoso);
        Assert.Null(repo.BajaId);
    }

    [Fact]
    public async Task DarDeBajaAsync_ActorVendedor_Rechaza()
    {
        var destino = Usuario(2);
        var repo = new SeguridadRepositoryFake(Usuario(1, RolesSistema.Vendedor), destino);
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));

        Assert.False((await servicio.DarDeBajaAsync(destino.PublicId)).Exitoso);
        Assert.Null(repo.BajaId);
    }

    [Fact]
    public async Task DarDeBajaAsync_UsuarioInexistente_NoEncontrado()
    {
        var servicio = new UsuarioSeguridadService(new SeguridadRepositoryFake(Usuario(1)),
            new ConexionBajaFake(), new UsuarioActualFake(1));
        Assert.True((await servicio.DarDeBajaAsync(Guid.NewGuid())).NoEncontrado);
    }

    [Fact]
    public async Task DarDeBajaAsync_YaInactivo_NoActualiza()
    {
        var destino = Usuario(2, RolesSistema.Vendedor, EstadosUsuario.Inactivo);
        var repo = new SeguridadRepositoryFake(Usuario(1), destino);
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));
        Assert.False((await servicio.DarDeBajaAsync(destino.PublicId)).Exitoso);
        Assert.Null(repo.BajaId);
    }

    [Fact]
    public async Task DarDeBajaAsync_ErrorSql_RevierteTransaccion()
    {
        var destino = Usuario(2, RolesSistema.Vendedor);
        var repo = new SeguridadRepositoryFake(Usuario(1), destino) { FallarActualizacion = true };
        var conexion = new ConexionBajaFake();
        var servicio = new UsuarioSeguridadService(repo, conexion, new UsuarioActualFake(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.DarDeBajaAsync(destino.PublicId));
        Assert.True(conexion.Transaccion!.Revertida);
        Assert.False(conexion.Transaccion.Confirmada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task DarDeBajaAsync_SinIdentidad_Rechaza(int? id)
    {
        var repo = new SeguridadRepositoryFake();
        var conexion = new ConexionBajaFake();
        var servicio = new UsuarioSeguridadService(repo, conexion, new UsuarioActualFake(id));
        Assert.False((await servicio.DarDeBajaAsync(Guid.NewGuid())).Exitoso);
        Assert.Null(conexion.Transaccion);
    }

    [Fact]
    public async Task SesionActivaAsync_ReconsultaEstadoEnCadaPeticion()
    {
        var repo = new SeguridadRepositoryFake();
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));
        Assert.True(await servicio.SesionActivaAsync(1));
        repo.Activo = false;
        Assert.False(await servicio.SesionActivaAsync(1));
        Assert.Equal(2, repo.Consultas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SesionActivaAsync_IdInvalido_NoConsulta(int id)
    {
        var repo = new SeguridadRepositoryFake();
        var servicio = new UsuarioSeguridadService(repo, new ConexionBajaFake(), new UsuarioActualFake(1));
        Assert.False(await servicio.SesionActivaAsync(id));
        Assert.Equal(0, repo.Consultas);
    }
}
