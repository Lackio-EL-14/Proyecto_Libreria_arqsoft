using System.Data;
using System.Data.Common;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Domain;
using MySqlConnector;

namespace Libreria.Servicio.Clientes.Infrastructure.Repositories;

/// <summary>
/// Adapter: implementa el puerto <see cref="IClienteRepository"/> sobre MySQL 8.
/// Traduce el error 1062 (clave única repetida) a <see cref="ValorDuplicadoException"/>.
/// </summary>
public class ClienteRepository : IClienteRepository
{
    private const string RestriccionCiNit = "UQ_Cliente_CiNit";

    private const string ColumnasCliente = @"
            ClienteId, PublicId, CiNit, RazonSocial, Correo, Estado,
            FechaCreacion, UsuarioCreacionId, FechaModificacion, UsuarioModificacionId";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;

    public ClienteRepository(IDbConnectionFactory connectionFactory, IUsuarioActual usuarioActual)
    {
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(FiltroClientes filtro)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        // El ORDER BY sale de una lista cerrada (enum), nunca del texto del usuario.
        command.CommandText = $@"
            SELECT {ColumnasCliente}
            FROM Cliente
            WHERE (@Estado IS NULL OR Estado = @Estado)
              AND (@Busqueda IS NULL
                   OR CiNit LIKE CONCAT('%', @Busqueda, '%')
                   OR RazonSocial LIKE CONCAT('%', @Busqueda, '%')
                   OR Correo LIKE CONCAT('%', @Busqueda, '%'))
            ORDER BY {ColumnaOrden(filtro.Orden)} {(filtro.Descendente ? "DESC" : "ASC")}, ClienteId";

        AgregarParametro(command, "@Estado", filtro.Estado.HasValue ? filtro.Estado.Value : DBNull.Value);
        AgregarParametro(
            command,
            "@Busqueda",
            string.IsNullOrWhiteSpace(filtro.Busqueda) ? DBNull.Value : EscaparLike(filtro.Busqueda.Trim()));

        return await LeerClientesAsync(command);
    }

    public Task<IReadOnlyList<Cliente>> ObtenerActivasAsync(string? busqueda = null) =>
        ListarAsync(new FiltroClientes { Busqueda = busqueda, Estado = true });

    public async Task<Cliente?> ObtenerPorPublicIdAsync(Guid publicId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = $@"
            SELECT {ColumnasCliente}
            FROM Cliente
            WHERE PublicId = @PublicId";

        AgregarParametroGuid(command, "@PublicId", publicId);

        return (await LeerClientesAsync(command)).SingleOrDefault();
    }

    public async Task<Cliente?> ObtenerPorPublicIdAsync(Guid publicId, bool estadoEsperado)
    {
        var cliente = await ObtenerPorPublicIdAsync(publicId);
        return cliente?.Estado == estadoEsperado ? cliente : null;
    }

    public async Task CrearAsync(Cliente cliente)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Cliente
                (PublicId, CiNit, RazonSocial, Correo, Estado, FechaCreacion, UsuarioCreacionId)
            VALUES
                (@PublicId, @CiNit, @RazonSocial, @Correo, 1, NOW(6), @UsuarioCreacionId);
            SELECT LAST_INSERT_ID();";

        AgregarParametroGuid(command, "@PublicId", cliente.PublicId);
        AgregarDatosCliente(command, cliente);
        AgregarParametroGuid(command, "@UsuarioCreacionId", _usuarioActual.UsuarioId);

        var clienteId = await EjecutarAsync(() => command.ExecuteScalarAsync());

        cliente.ClienteId = Convert.ToInt32(clienteId);
        cliente.Estado = true;
        cliente.UsuarioCreacionId = _usuarioActual.UsuarioId;
    }

    public async Task<bool> ActualizarAsync(Cliente cliente)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE Cliente
            SET CiNit = @CiNit,
                RazonSocial = @RazonSocial,
                Correo = @Correo,
                FechaModificacion = NOW(6),
                UsuarioModificacionId = @UsuarioModificacionId
            WHERE PublicId = @PublicId
              AND Estado = 1";

        AgregarDatosCliente(command, cliente);
        AgregarParametroGuid(command, "@UsuarioModificacionId", _usuarioActual.UsuarioId);
        AgregarParametroGuid(command, "@PublicId", cliente.PublicId);

        return await EjecutarAsync(() => command.ExecuteNonQueryAsync()) > 0;
    }

    public async Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE Cliente
            SET Estado = @Estado,
                FechaModificacion = NOW(6),
                UsuarioModificacionId = @UsuarioModificacionId
            WHERE PublicId = @PublicId";

        AgregarParametro(command, "@Estado", nuevoEstado);
        AgregarParametroGuid(command, "@UsuarioModificacionId", _usuarioActual.UsuarioId);
        AgregarParametroGuid(command, "@PublicId", publicId);

        // Filas encontradas (UseAffectedRows = false en MySqlConnectionFactory).
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null)
    {
        if (columna != IClienteRepository.ColumnaCiNit)
        {
            throw new ArgumentException("La columna indicada no es válida.", nameof(columna));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        // Se cuentan también los inactivos: el CI/NIT es único en toda la tabla.
        command.CommandText = @"
            SELECT COUNT(1)
            FROM Cliente
            WHERE CiNit = @Valor
              AND (@ExcluirPublicId IS NULL OR PublicId <> @ExcluirPublicId)";

        AgregarParametro(command, "@Valor", valor.Trim());
        AgregarParametroGuid(command, "@ExcluirPublicId", excluirPublicId);

        return Convert.ToInt64(await command.ExecuteScalarAsync()) > 0;
    }

    /// <summary>El agregado Cliente no tiene relaciones dependientes en su propia base.</summary>
    public Task<bool> TieneRelacionesAsync(Guid publicId) => Task.FromResult(false);

    private static async Task<T> EjecutarAsync<T>(Func<Task<T>> escritura)
    {
        try
        {
            return await escritura();
        }
        catch (MySqlException error) when (EsCiNitDuplicado(error))
        {
            throw new ValorDuplicadoException(IClienteRepository.ColumnaCiNit, error);
        }
    }

    private static bool EsCiNitDuplicado(MySqlException error) =>
        error.ErrorCode == MySqlErrorCode.DuplicateKeyEntry
        && error.Message.Contains(RestriccionCiNit, StringComparison.OrdinalIgnoreCase);

    private static async Task<IReadOnlyList<Cliente>> LeerClientesAsync(DbCommand command)
    {
        var clientes = new List<Cliente>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            clientes.Add(MapearCliente(reader));
        }

        return clientes;
    }

    private static Cliente MapearCliente(DbDataReader reader) => new()
    {
        ClienteId = reader.GetInt32(reader.GetOrdinal("ClienteId")),
        PublicId = LeerGuid(reader, "PublicId") ?? Guid.Empty,
        CiNit = reader.GetString(reader.GetOrdinal("CiNit")),
        RazonSocial = reader.GetString(reader.GetOrdinal("RazonSocial")),
        Correo = LeerTexto(reader, "Correo"),
        Estado = Convert.ToBoolean(reader.GetValue(reader.GetOrdinal("Estado"))),
        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
        UsuarioCreacionId = LeerGuid(reader, "UsuarioCreacionId"),
        FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("FechaModificacion")),
        UsuarioModificacionId = LeerGuid(reader, "UsuarioModificacionId")
    };

    private static string? LeerTexto(DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    /// <summary>CHAR(36): según la configuración del conector llega como Guid o como texto.</summary>
    private static Guid? LeerGuid(DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetValue(ordinal) switch
        {
            Guid guid => guid,
            var valor => Guid.Parse(Convert.ToString(valor)!)
        };
    }

    private static string ColumnaOrden(OrdenClientes orden) => orden switch
    {
        OrdenClientes.CiNit => "CiNit",
        OrdenClientes.Correo => "Correo",
        OrdenClientes.FechaCreacion => "FechaCreacion",
        _ => "RazonSocial"
    };

    /// <summary>Los comodines escritos por el usuario se buscan literalmente.</summary>
    private static string EscaparLike(string texto) =>
        texto.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    private static void AgregarDatosCliente(DbCommand command, Cliente cliente)
    {
        AgregarParametro(command, "@CiNit", cliente.CiNit);
        AgregarParametro(command, "@RazonSocial", cliente.RazonSocial);
        AgregarParametro(command, "@Correo", cliente.Correo ?? (object)DBNull.Value);
    }

    /// <summary>Los GUID se guardan como texto de 36 caracteres (columnas CHAR(36)).</summary>
    private static void AgregarParametroGuid(DbCommand command, string nombre, Guid? valor) =>
        AgregarParametro(command, nombre, valor.HasValue ? valor.Value.ToString("D") : DBNull.Value);

    private static void AgregarParametro(DbCommand command, string nombre, object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}
