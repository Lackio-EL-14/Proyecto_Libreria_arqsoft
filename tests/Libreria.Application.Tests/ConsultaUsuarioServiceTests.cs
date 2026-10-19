using System.Data;
using System.Text.Json;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Services;
using Libreria.Domain;

namespace Libreria.Application.Tests;

public class ConsultaUsuarioServiceTests
{
    [Fact]
    public async Task ObtenerListadoAsync_DevuelveDatosPermitidosDeTodosLosUsuarios()
    {
        var esperado = CrearUsuario(EstadosUsuario.Activo);
        var repository = new UsuarioListadoRepositoryFake(esperado);
        var service = new ConsultaUsuarioService(repository);

        var resultado = await service.ObtenerListadoAsync();

        Assert.Equal(esperado, Assert.Single(resultado));
        Assert.Null(repository.UltimoEstado);
        Assert.Equal(1, repository.Consultas);
    }

    [Theory]
    [InlineData(EstadosUsuario.Activo)]
    [InlineData(EstadosUsuario.Pendiente)]
    [InlineData(EstadosUsuario.Inactivo)]
    public async Task ObtenerListadoAsync_FiltraPorCadaEstado(string estado)
    {
        var usuarios = EstadosUsuario.Todos.Select(CrearUsuario).ToArray();
        var repository = new UsuarioListadoRepositoryFake(usuarios);
        var service = new ConsultaUsuarioService(repository);

        var resultado = await service.ObtenerListadoAsync(estado);

        Assert.Equal(estado, Assert.Single(resultado).Estado);
        Assert.Equal(estado, repository.UltimoEstado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ObtenerListadoAsync_SinFiltro_ConsultaTodos(string? estado)
    {
        var repository = new UsuarioListadoRepositoryFake(CrearUsuario(EstadosUsuario.Activo));
        var service = new ConsultaUsuarioService(repository);

        Assert.Single(await service.ObtenerListadoAsync(estado));
        Assert.Null(repository.UltimoEstado);
    }

    [Fact]
    public async Task ObtenerListadoAsync_NormalizaEspaciosDelFiltro()
    {
        var repository = new UsuarioListadoRepositoryFake(CrearUsuario(EstadosUsuario.Activo));
        var service = new ConsultaUsuarioService(repository);

        Assert.Single(await service.ObtenerListadoAsync(" Activo "));
        Assert.Equal(EstadosUsuario.Activo, repository.UltimoEstado);
    }

    [Theory]
    [InlineData("Desconocido")]
    [InlineData("Activo'; DROP TABLE Usuario; --")]
    public async Task ObtenerListadoAsync_EstadoInvalido_NoConsultaElRepositorio(string estado)
    {
        var repository = new UsuarioListadoRepositoryFake();
        var service = new ConsultaUsuarioService(repository);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ObtenerListadoAsync(estado));

        Assert.StartsWith("Seleccione un estado de usuario válido.", error.Message);
        Assert.Equal(0, repository.Consultas);
    }

    [Fact]
    public async Task ObtenerListadoAsync_SinUsuarios_DevuelveListaVacia()
    {
        var service = new ConsultaUsuarioService(new UsuarioListadoRepositoryFake());

        Assert.Empty(await service.ObtenerListadoAsync());
    }

    [Fact]
    public async Task ObtenerListadoAsync_JsonSoloContieneCamposPermitidosSinCredencialesNiIdsInternos()
    {
        var esperado = CrearUsuario(EstadosUsuario.Pendiente);
        var service = new ConsultaUsuarioService(new UsuarioListadoRepositoryFake(esperado));

        using var json = JsonSerializer.SerializeToDocument(await service.ObtenerListadoAsync());
        var usuario = json.RootElement[0];
        var permitidos = new[]
        {
            "PublicId", "NombreCompleto", "NombreUsuario", "Correo",
            "Rol", "Estado", "FechaCreacion", "CreadoPor"
        };

        Assert.Equal(permitidos.OrderBy(p => p),
            usuario.EnumerateObject().Select(p => p.Name).OrderBy(p => p));
        Assert.Equal(esperado.PublicId, usuario.GetProperty("PublicId").GetGuid());
        Assert.Equal(esperado.NombreCompleto, usuario.GetProperty("NombreCompleto").GetString());
        Assert.Equal(esperado.NombreUsuario, usuario.GetProperty("NombreUsuario").GetString());
        Assert.Equal(esperado.Correo, usuario.GetProperty("Correo").GetString());
        Assert.Equal(esperado.Rol, usuario.GetProperty("Rol").GetString());
        Assert.Equal(esperado.Estado, usuario.GetProperty("Estado").GetString());
        Assert.Equal(esperado.FechaCreacion, usuario.GetProperty("FechaCreacion").GetDateTime());
        Assert.Equal(esperado.CreadoPor, usuario.GetProperty("CreadoPor").GetString());
    }

    [Fact]
    public async Task ObtenerListadoAsync_UsuarioSinCreador_ConservaElRegistro()
    {
        var usuario = CrearUsuario(EstadosUsuario.Activo) with { CreadoPor = null };
        var service = new ConsultaUsuarioService(new UsuarioListadoRepositoryFake(usuario));

        Assert.Null(Assert.Single(await service.ObtenerListadoAsync()).CreadoPor);
    }

    internal static UsuarioListItemModel CrearUsuario(string estado) => new(
        Guid.NewGuid(), "Ana Pérez", "aperez", "ana@example.test",
        RolesSistema.Vendedor, estado, new DateTime(2026, 10, 19, 10, 30, 0), "Administrador");
}

internal sealed class UsuarioListadoRepositoryFake(params UsuarioListItemModel[] usuarios) : IUsuarioRepository
{
    public string? HashActivo { get; set; }
    public int? UsuarioContrasenaId { get; private set; }
    public string? NuevoHash { get; private set; }
    public int CambiosContrasena { get; private set; }
    public Task<string?> ObtenerHashActivoAsync(int usuarioId)
    {
        UsuarioContrasenaId = usuarioId;
        return Task.FromResult(HashActivo);
    }
    public Task<bool> ActualizarContrasenaAsync(int usuarioId, string hashAnterior, string nuevoHash)
    {
        UsuarioContrasenaId = usuarioId;
        NuevoHash = nuevoHash;
        CambiosContrasena++;
        return Task.FromResult(hashAnterior == HashActivo);
    }
    public string? UltimoEstado { get; private set; }
    public int Consultas { get; private set; }

    public Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null)
    {
        UltimoEstado = estado;
        Consultas++;
        IReadOnlyList<UsuarioListItemModel> resultado = usuarios
            .Where(u => estado is null || u.Estado == estado).ToList();
        return Task.FromResult(resultado);
    }

    public Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario)
        => throw new NotImplementedException();
    public Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId)
        => throw new NotImplementedException();
    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        => throw new NotImplementedException();
    public Task<bool> ExisteCorreoAsync(string correo)
        => throw new NotImplementedException();
    public Task<int> InsertarAsync(Usuario usuario)
        => throw new NotImplementedException();
    public Task<int> InsertarAsync(Usuario usuario, IDbConnection connection, IDbTransaction transaction)
        => throw new NotImplementedException();
}
