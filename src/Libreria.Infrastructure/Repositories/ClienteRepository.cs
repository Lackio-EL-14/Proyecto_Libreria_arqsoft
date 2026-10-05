using Libreria.Application.Domain;
using Libreria.Application.Ports.Secondary;
using System.Data;
using System.Data.Common;

namespace Libreria.Infrastructure;

public class ClienteRepository : IClienteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClienteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Cliente>> BuscarPorCiNitAsync(string ciNit)
    {
        var clientes = new List<Cliente>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT ClienteId, CiNit, RazonSocial, Correo
            FROM Cliente
            WHERE CiNit LIKE @CiNit + '%'
            ORDER BY RazonSocial";

        AgregarParametro(command, "@CiNit", ciNit.Trim());

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            clientes.Add(MapearCliente(reader));
        }

        return clientes;
    }

    public async Task<Cliente> CrearRapidoAsync(Cliente cliente)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Cliente (CiNit, RazonSocial, Correo)
            OUTPUT INSERTED.ClienteId
            VALUES (@CiNit, @RazonSocial, @Correo)";

        AgregarDatosCliente(command, cliente);

        var resultado = await command.ExecuteScalarAsync();

        cliente.ClienteId = Convert.ToInt32(resultado);

        return cliente;
    }

    public async Task<bool> ExisteCiNitAsync(string ciNit)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT COUNT(1)
            FROM Cliente
            WHERE CiNit = @CiNit";

        AgregarParametro(command, "@CiNit", ciNit.Trim());

        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private async Task<DbConnection> CrearConexionAbiertaAsync()
    {
        var connection = _connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    private static Cliente MapearCliente(DbDataReader reader)
    {
        return new Cliente
        {
            ClienteId = reader.GetInt32(reader.GetOrdinal("ClienteId")),
            CiNit = reader.GetString(reader.GetOrdinal("CiNit")),
            RazonSocial = reader.GetString(reader.GetOrdinal("RazonSocial")),
            Correo = reader.IsDBNull(reader.GetOrdinal("Correo"))
                ? null
                : reader.GetString(reader.GetOrdinal("Correo"))
        };
    }

    private static void AgregarDatosCliente(DbCommand command, Cliente cliente)
    {
        AgregarParametro(command, "@CiNit", cliente.CiNit);
        AgregarParametro(command, "@RazonSocial", cliente.RazonSocial);
        AgregarParametro(
            command,
            "@Correo",
            cliente.Correo ?? (object)DBNull.Value);
    }

    private static void AgregarParametro(
        DbCommand command,
        string nombre,
        object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}