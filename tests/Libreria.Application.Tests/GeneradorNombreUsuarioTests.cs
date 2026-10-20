using Libreria.Application.Ports.Secondary;
using Libreria.Application.Services;
using Libreria.Domain;
using System.Data;

namespace Libreria.Application.Tests;

public class GeneradorNombreUsuarioTests
{
    [Fact]
    public async Task GenerarAsync_QuitaTildesYEnie()
    {
        var repository = new UsuarioRepositoryFake();
        var generador = new GeneradorNombreUsuario(repository);

        var resultado = await generador.GenerarAsync("José", "Peña");

        Assert.Equal("jpena", resultado);
    }

    [Fact]
    public async Task GenerarAsync_AceptaApellidoCompuesto()
    {
        var repository = new UsuarioRepositoryFake();
        var generador = new GeneradorNombreUsuario(repository);

        var resultado = await generador.GenerarAsync(
            "María José",
            "De la Cruz");

        Assert.Equal("mdelacruz", resultado);
    }

    [Fact]
    public async Task GenerarAsync_NormalizaNombreConTildeYEnie()
    {
        var repository = new UsuarioRepositoryFake();
        var generador = new GeneradorNombreUsuario(repository);

        var resultado = await generador.GenerarAsync(
            "Álvaro",
            "Núñez");

        Assert.Equal("anunez", resultado);
    }

    [Fact]
    public async Task GenerarAsync_AgregaNumeroCuandoHayColisiones()
    {
        var repository = new UsuarioRepositoryFake(
            "jpena",
            "jpena2",
            "jpena3");

        var generador = new GeneradorNombreUsuario(repository);

        var resultado = await generador.GenerarAsync("José", "Peña");

        Assert.Equal("jpena4", resultado);
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Task<string?> ObtenerHashActivoAsync(int usuarioId) => throw new NotSupportedException();
        public Task<bool> ActualizarContrasenaAsync(int usuarioId, string hashAnterior, string nuevoHash) => throw new NotSupportedException();
        private readonly HashSet<string> _usuarios;

        public UsuarioRepositoryFake(params string[] usuarios)
        {
            _usuarios = new HashSet<string>(
                usuarios,
                StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        {
            return Task.FromResult(_usuarios.Contains(nombreUsuario));
        }

        public Task<IReadOnlyList<Libreria.Application.Models.UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null)
            => throw new NotImplementedException();

        public Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario)
            => throw new NotImplementedException();

        public Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId)
            => throw new NotImplementedException();

        public Task<bool> ExisteCorreoAsync(string correo)
            => throw new NotImplementedException();

        public Task<int> InsertarAsync(Usuario usuario)
            => throw new NotImplementedException();

        public Task<int> InsertarAsync(
            Usuario usuario,
            IDbConnection connection,
            IDbTransaction transaction)
            => throw new NotImplementedException();

        public Task<Usuario?> ObtenerPorTokenActivacionHashAsync(string tokenHash)
    => throw new NotImplementedException();

        public Task<bool> ActivarAsync(
            int usuarioId,
            string passwordHash,
            DateTime fechaActivacion)
            => throw new NotImplementedException();
    }
}
