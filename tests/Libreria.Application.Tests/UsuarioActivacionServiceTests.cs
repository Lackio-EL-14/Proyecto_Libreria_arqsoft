using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Services;
using Libreria.Application.Validators;
using Libreria.Domain;
using System.Data;

namespace Libreria.Application.Tests;

public class UsuarioActivacionServiceTests
{
    [Fact]
    public async Task ActivarAsync_TokenValido_ActivaUsuario()
    {
        var token = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(token);

        var usuario = new Usuario
        {
            UsuarioId = 10,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,
            TokenActivacionExpiracion = DateTime.UtcNow.AddHours(1),
            TokenActivacionFechaUso = null
        };

        var repositorio = new UsuarioRepositoryFake(usuario);
        var hasher = new ContrasenaHasherFake();

        var servicio = new UsuarioService(
            repositorio,
            null!,
            null!,
            null!,
            null!,
            null!,
            hasher,
            new PasswordPolicyValidator());

        var input = new UsuarioActivacionInput
        {
            TokenHash = tokenHash,
            Token = token,
            Contrasena = "Segura123!",
            ConfirmarContrasena = "Segura123!"
        };

        await servicio.ActivarAsync(input);

        Assert.True(repositorio.ActivarFueLlamado);
        Assert.Equal(10, repositorio.UsuarioIdActivado);
        Assert.Equal("HASH_PRUEBA", repositorio.PasswordHashRecibido);
    }

    private sealed class ContrasenaHasherFake : IContrasenaHasher
    {
        public string Hashear(string contrasena)
            => "HASH_PRUEBA";

        public bool Verificar(string hash, string contrasena)
            => throw new NotImplementedException();

        public bool RequiereRehash(string hash) => false;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        private readonly Usuario _usuario;

        public bool ActivarFueLlamado { get; private set; }
        public int UsuarioIdActivado { get; private set; }
        public string? PasswordHashRecibido { get; private set; }

        public UsuarioRepositoryFake(Usuario usuario)
        {
            _usuario = usuario;
        }

        public Task<Usuario?> ObtenerPorTokenActivacionHashAsync(
            string tokenHash)
        {
            return Task.FromResult<Usuario?>(_usuario);
        }

        public Task<bool> ActivarAsync(
            int usuarioId,
            string passwordHash,
            DateTime fechaActivacion)
        {
            ActivarFueLlamado = true;
            UsuarioIdActivado = usuarioId;
            PasswordHashRecibido = passwordHash;

            return Task.FromResult(true);
        }

        public Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(
            string nombreUsuario)
            => throw new NotImplementedException();

        public Task<string?> ObtenerNombreCompletoPorIdAsync(
            int usuarioId)
            => throw new NotImplementedException();

        public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
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
    }

    [Fact]
    public async Task ActivarAsync_TokenExpirado_NoActivaUsuario()
    {
        var token = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(token);

        var usuario = new Usuario
        {
            UsuarioId = 10,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,

            // Ya venció hace un minuto
            TokenActivacionExpiracion = DateTime.UtcNow.AddMinutes(-1),

            TokenActivacionFechaUso = null
        };

        var repositorio = new UsuarioRepositoryFake(usuario);

        var servicio = new UsuarioService(
            repositorio,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ContrasenaHasherFake(),
            new PasswordPolicyValidator());

        var input = new UsuarioActivacionInput
        {
            TokenHash = tokenHash,
            Token = token,
            Contrasena = "Segura123!",
            ConfirmarContrasena = "Segura123!"
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ActivarAsync(input));

        Assert.Equal(
            "El enlace de activación no es válido o ha expirado.",
            error.Message);

        Assert.False(repositorio.ActivarFueLlamado);
    }

    [Fact]
    public async Task ActivarAsync_TokenYaUsado_NoActivaUsuario()
    {
        var token = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(token);

        var usuario = new Usuario
        {
            UsuarioId = 10,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,
            TokenActivacionExpiracion = DateTime.UtcNow.AddHours(1),

            // Simula que el enlace ya fue usado anteriormente
            TokenActivacionFechaUso = DateTime.UtcNow.AddMinutes(-5)
        };

        var repositorio = new UsuarioRepositoryFake(usuario);

        var servicio = new UsuarioService(
            repositorio,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ContrasenaHasherFake(),
            new PasswordPolicyValidator());

        var input = new UsuarioActivacionInput
        {
            TokenHash = tokenHash,
            Token = token,
            Contrasena = "Segura123!",
            ConfirmarContrasena = "Segura123!"
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ActivarAsync(input));

        Assert.Equal(
            "El enlace de activación no es válido o ha expirado.",
            error.Message);

        Assert.False(repositorio.ActivarFueLlamado);
    }

    [Fact]
    public async Task ActivarAsync_TokenAlterado_NoActivaUsuario()
    {
        var tokenOriginal = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(tokenOriginal);

        var usuario = new Usuario
        {
            UsuarioId = 10,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,
            TokenActivacionExpiracion = DateTime.UtcNow.AddHours(1),
            TokenActivacionFechaUso = null
        };

        var repositorio = new UsuarioRepositoryFake(usuario);

        var servicio = new UsuarioService(
            repositorio,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ContrasenaHasherFake(),
            new PasswordPolicyValidator());

        var input = new UsuarioActivacionInput
        {
            TokenHash = tokenHash,

            // Este token NO es el original
            Token = TokenActivacionService.GenerarToken(),

            Contrasena = "Segura123!",
            ConfirmarContrasena = "Segura123!"
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ActivarAsync(input));

        Assert.Equal(
            "El enlace de activación no es válido o ha expirado.",
            error.Message);

        Assert.False(repositorio.ActivarFueLlamado);
    }

    [Fact]
    public async Task ActivarAsync_ContrasenasNoCoinciden_NoActivaUsuario()
    {
        var token = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(token);

        var usuario = new Usuario
        {
            UsuarioId = 10,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,
            TokenActivacionExpiracion = DateTime.UtcNow.AddHours(1),
            TokenActivacionFechaUso = null
        };

        var repositorio = new UsuarioRepositoryFake(usuario);

        var servicio = new UsuarioService(
            repositorio,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ContrasenaHasherFake(),
            new PasswordPolicyValidator());

        var input = new UsuarioActivacionInput
        {
            TokenHash = tokenHash,
            Token = token,
            Contrasena = "Segura123!",
            ConfirmarContrasena = "Otra123!"
        };

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.ActivarAsync(input));

        Assert.Equal(
            "La contraseña y su confirmación no coinciden.",
            error.Message);

        Assert.False(repositorio.ActivarFueLlamado);
    }

    [Fact]
    public async Task ActivarAsync_ContrasenaNoCumplePolitica_NoActivaUsuario()
    {
        var token = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(token);

        var usuario = new Usuario
        {
            UsuarioId = 10,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,
            TokenActivacionExpiracion = DateTime.UtcNow.AddHours(1),
            TokenActivacionFechaUso = null
        };

        var repositorio = new UsuarioRepositoryFake(usuario);

        var servicio = new UsuarioService(
            repositorio,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ContrasenaHasherFake(),
            new PasswordPolicyValidator());

        var input = new UsuarioActivacionInput
        {
            TokenHash = tokenHash,
            Token = token,

            // No cumple longitud, mayúscula, número ni carácter especial
            Contrasena = "abc",
            ConfirmarContrasena = "abc"
        };

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.ActivarAsync(input));

        Assert.Contains(
            "La contraseña debe tener al menos 8 caracteres.",
            error.Message);

        Assert.False(repositorio.ActivarFueLlamado);
    }

}
