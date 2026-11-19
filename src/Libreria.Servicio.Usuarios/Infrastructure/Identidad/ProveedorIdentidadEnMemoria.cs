using System.Collections.Concurrent;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain;

namespace Libreria.Servicio.Usuarios.Infrastructure.Identidad;

/// <summary>
/// Doble de <see cref="IProveedorIdentidad"/> que imita a Keycloak sin red: misma política de
/// contraseñas (incluida <c>notUsername</c>), contraseña temporal con cambio pendiente, roles
/// <c>Administrador</c>/<c>Vendedor</c> y los usuarios semilla con sus GUID fijos.
/// </summary>
/// <remarks>
/// Sirve para las pruebas de US-128 a US-131 y para correr el MS sin Keycloak con
/// <c>ProveedorIdentidad:Modo = EnMemoria</c>. Los usuarios semilla empiezan sin contraseña;
/// se fija con <see cref="FijarContrasenaAsync"/>. No usar fuera de desarrollo.
/// </remarks>
public sealed class ProveedorIdentidadEnMemoria : IProveedorIdentidad
{
    public static readonly Guid IdAdminSemilla = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid IdVendedorSemilla = Guid.Parse("a0000000-0000-0000-0000-000000000002");

    private static readonly HashSet<string> RolesRealm = new(StringComparer.Ordinal) { "Administrador", "Vendedor" };

    private readonly ConcurrentDictionary<Guid, UsuarioEnMemoria> _usuarios = new();
    private readonly PasswordPolicyValidator _politica = new();

    public ProveedorIdentidadEnMemoria()
    {
        Agregar(IdAdminSemilla, new UsuarioIdentidad("admin", "admin@libreria.local", "Administrador", "Sistema"), "Administrador");
        Agregar(IdVendedorSemilla, new UsuarioIdentidad("vendedor", "vendedor@libreria.local", "Vendedor", "Mostrador"), "Vendedor");
    }

    /// <summary>Vista de solo lectura para que las pruebas comprueben el estado.</summary>
    public IReadOnlyDictionary<Guid, UsuarioEnMemoria> Usuarios => _usuarios;

    /// <summary>Si se asigna, la siguiente llamada lanza esta excepción (para simular Keycloak caído).</summary>
    public Exception? FallarSiguienteLlamada { get; set; }

    public Task<Result<Guid>> CrearUsuarioAsync(UsuarioIdentidad usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        LanzarSiCorresponde();

        lock (_usuarios)
        {
            var duplicado = _usuarios.Values.Any(u =>
                string.Equals(u.Datos.NombreUsuario, usuario.NombreUsuario, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Datos.Correo, usuario.Correo, StringComparison.OrdinalIgnoreCase));

            if (duplicado)
            {
                return Task.FromResult(Result<Guid>.Fallo(
                    Result.Conflicto(nameof(usuario.NombreUsuario), MensajesIdentidad.UsuarioDuplicado)));
            }

            var id = Guid.NewGuid();
            Agregar(id, usuario, rol: null);
            return Task.FromResult(Result<Guid>.Exito(id));
        }
    }

    public Task<Result> AsignarRolAsync(Guid idUsuario, string rol, CancellationToken cancellationToken = default) =>
        ConUsuario(idUsuario, usuario =>
        {
            if (!RolesRealm.Contains(rol))
            {
                return Result.Validacion("Rol", MensajesIdentidad.RolInvalido);
            }

            usuario.Roles.Add(rol);
            return Result.Exito();
        });

    public Task<Result> FijarContrasenaAsync(Guid idUsuario, string contrasena, bool temporal, CancellationToken cancellationToken = default) =>
        ConUsuario(idUsuario, usuario =>
        {
            var errores = _politica.Validate(contrasena);
            if (errores.Count > 0)
            {
                // Keycloak informa solo la primera regla incumplida; aquí se imita.
                return Result.Validacion("Contrasena", errores[0]);
            }

            if (string.Equals(contrasena, usuario.Datos.NombreUsuario, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Validacion("Contrasena", MensajesIdentidad.DePolitica("invalidPasswordNotUsernameMessage"));
            }

            usuario.Contrasena = contrasena;
            usuario.CambioPendiente = temporal;
            return Result.Exito();
        });

    public Task<Result> VerificarContrasenaAsync(string nombreUsuario, string contrasena, CancellationToken cancellationToken = default)
    {
        LanzarSiCorresponde();
        var usuario = _usuarios.Values.FirstOrDefault(u =>
            string.Equals(u.Datos.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase));

        if (usuario?.Contrasena is null || !string.Equals(usuario.Contrasena, contrasena, StringComparison.Ordinal))
        {
            return Task.FromResult(Result.Validacion("ContrasenaActual", MensajesIdentidad.ContrasenaActualIncorrecta));
        }

        if (!usuario.Habilitado)
        {
            return Task.FromResult(Result.Validacion("ContrasenaActual", MensajesIdentidad.CuentaDeshabilitada));
        }

        return Task.FromResult(usuario.CambioPendiente
            ? Result.Validacion("ContrasenaActual", MensajesIdentidad.CambioPendiente)
            : Result.Exito());
    }

    public Task<Result> HabilitarAsync(Guid idUsuario, CancellationToken cancellationToken = default) =>
        ConUsuario(idUsuario, usuario =>
        {
            usuario.Habilitado = true;
            return Result.Exito();
        });

    public Task<Result> DeshabilitarAsync(Guid idUsuario, CancellationToken cancellationToken = default) =>
        ConUsuario(idUsuario, usuario =>
        {
            usuario.Habilitado = false;
            return Result.Exito();
        });

    public Task<Result<bool>> TieneCambioDeContrasenaPendienteAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        LanzarSiCorresponde();
        return Task.FromResult(_usuarios.TryGetValue(idUsuario, out var usuario)
            ? Result<bool>.Exito(usuario.CambioPendiente)
            : Result<bool>.Fallo(Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado)));
    }

    public Task<Result> EliminarUsuarioAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        LanzarSiCorresponde();
        _usuarios.TryRemove(idUsuario, out _);
        return Task.FromResult(Result.Exito());
    }

    private void Agregar(Guid id, UsuarioIdentidad datos, string? rol)
    {
        var usuario = new UsuarioEnMemoria(id, datos);
        if (rol is not null)
        {
            usuario.Roles.Add(rol);
        }

        _usuarios[id] = usuario;
    }

    private Task<Result> ConUsuario(Guid idUsuario, Func<UsuarioEnMemoria, Result> accion)
    {
        LanzarSiCorresponde();
        if (!_usuarios.TryGetValue(idUsuario, out var usuario))
        {
            return Task.FromResult(Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado));
        }

        lock (usuario)
        {
            return Task.FromResult(accion(usuario));
        }
    }

    private void LanzarSiCorresponde()
    {
        if (FallarSiguienteLlamada is { } excepcion)
        {
            FallarSiguienteLlamada = null;
            throw excepcion;
        }
    }

    public sealed class UsuarioEnMemoria
    {
        internal UsuarioEnMemoria(Guid id, UsuarioIdentidad datos)
        {
            Id = id;
            Datos = datos;
        }

        public Guid Id { get; }
        public UsuarioIdentidad Datos { get; }
        public HashSet<string> Roles { get; } = new(StringComparer.Ordinal);
        public bool Habilitado { get; internal set; } = true;
        public bool CambioPendiente { get; internal set; }
        internal string? Contrasena { get; set; }
        public bool TieneContrasena => Contrasena is not null;
    }
}
