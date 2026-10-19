using System.Data;
using Libreria.Application.Factories;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Services;
using Libreria.Application.Validators;
using Libreria.Domain;

namespace Libreria.Application.Tests;

public class AutenticacionEstadoTests
{
    [Theory]
    [InlineData(EstadosUsuario.Inactivo)]
    [InlineData(EstadosUsuario.Pendiente)]
    [InlineData("Inexistente")]
    public async Task Login_EstadoSinAcceso_DevuelveMismoMensajeGenerico(string estado)
    {
        var repo = new LoginRepositoryFake(estado);
        var servicio = new AutenticacionService(new Factory(repo), new Hasher(), new LoginValidator());

        var resultado = await servicio.AutenticarAsync(new LoginInput
            { NombreUsuario = "prueba", Contrasena = "Prueba123!" });

        Assert.False(resultado.Exitoso);
        Assert.Null(resultado.Usuario);
        Assert.Equal(ResultadoAutenticacion.MensajeCredencialesInvalidas, Assert.Single(resultado.Errores).Value);
    }

    [Fact]
    public async Task Login_ActivoConContrasenaCorrecta_ConservaAcceso()
    {
        var servicio = new AutenticacionService(new Factory(new LoginRepositoryFake(EstadosUsuario.Activo)),
            new Hasher(), new LoginValidator());
        Assert.True((await servicio.AutenticarAsync(new LoginInput
            { NombreUsuario = "prueba", Contrasena = "Prueba123!" })).Exitoso);
    }

    private sealed class Factory(IUsuarioRepository repository) : UsuarioRepositoryFactory
    {
        public override IUsuarioRepository CrearRepositorio() => repository;
    }
    private sealed class Hasher : IContrasenaHasher
    {
        public string Hashear(string contrasena) => throw new NotSupportedException();
        public bool Verificar(string hash, string contrasena) => true;
    }
    private sealed class LoginRepositoryFake(string estado) : IUsuarioRepository
    {
        public Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario) => Task.FromResult(
            estado == "Inexistente" ? null : new Usuario
            {
                UsuarioId = 1, PublicId = Guid.NewGuid(), Estado = estado,
                NombreUsuario = "prueba", Rol = new Rol { Nombre = RolesSistema.Vendedor, Estado = true }
            });
        public Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null) => throw new NotSupportedException();
        public Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId) => throw new NotSupportedException();
        public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario) => throw new NotSupportedException();
        public Task<bool> ExisteCorreoAsync(string correo) => throw new NotSupportedException();
        public Task<int> InsertarAsync(Usuario usuario) => throw new NotSupportedException();
        public Task<int> InsertarAsync(Usuario usuario, IDbConnection connection, IDbTransaction transaction) => throw new NotSupportedException();
    }
}
