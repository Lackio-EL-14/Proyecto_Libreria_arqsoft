using Libreria.Servicio.Clientes.Application.Comun;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Application.Services;
using Libreria.Servicio.Clientes.Application.Validators;
using Libreria.Servicio.Clientes.Domain;
using Libreria.Servicio.Clientes.Infrastructure.Connection;
using Libreria.Servicio.Clientes.Infrastructure.Factories;
using Libreria.Servicio.Clientes.Infrastructure.Repositories;
using MySqlConnector;

namespace Libreria.Servicio.Clientes.Tests.Integracion;

/// <summary>
/// Adaptador MySQL y servicio contra la BD real con los datos semilla de
/// docs/contratos/datos-semilla.md. Cada prueba usa CI/NIT con prefijo propio y los borra al final.
/// </summary>
public sealed class ClienteRepositoryMySqlTests : IAsyncLifetime
{
    private const string Prefijo = "PRB114-";
    private static readonly Guid Admin = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid ClienteSemillaInactivo = Guid.Parse("c0000000-0000-0000-0000-000000000003");

    private readonly string _cadena = Environment.GetEnvironmentVariable(MySqlFactAttribute.Variable) ?? string.Empty;
    private IDbConnectionFactory _conexiones = null!;
    private ClienteRepository _repositorio = null!;
    private ClienteService _servicio = null!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_cadena))
        {
            return;
        }

        ConnectionStringSingleton.Instancia.Configurar(_cadena);
        _conexiones = new MySqlConnectionFactory(ConnectionStringSingleton.Instancia);
        var fabrica = new MySqlClienteRepositoryFactory(_conexiones, new UsuarioFijo(Admin));
        _repositorio = (ClienteRepository)fabrica.CrearRepositorio();
        _servicio = new ClienteService(fabrica, new ClienteValidator(fabrica));
        await BorrarDatosDePruebaAsync();
    }

    public Task DisposeAsync() =>
        string.IsNullOrWhiteSpace(_cadena) ? Task.CompletedTask : BorrarDatosDePruebaAsync();

    [MySqlFact]
    public async Task Los_datos_semilla_se_leen_con_guid_estado_y_auditoria()
    {
        var cliente = await _repositorio.ObtenerPorPublicIdAsync(ClienteSemillaInactivo);

        Assert.NotNull(cliente);
        Assert.Equal("1234567", cliente.CiNit);
        Assert.Equal("Juan Carlos Pérez Rojas", cliente.RazonSocial);
        Assert.Null(cliente.Correo);
        Assert.False(cliente.Estado);
        Assert.Equal(Admin, cliente.UsuarioCreacionId);
    }

    [MySqlFact]
    public async Task Alta_edicion_baja_y_reactivacion_de_punta_a_punta()
    {
        var alta = await _servicio.RegistrarAsync(Input("0001", "Prueba   Integración", "prueba@correo.bo"));
        Assert.True(alta.EsExito, string.Join("; ", alta.Errores.Values));
        var publicId = alta.Valor.PublicId;
        Assert.Equal("Prueba Integración", alta.Valor.RazonSocial);
        Assert.Equal(Admin, alta.Valor.UsuarioCreacionId);
        Assert.True(alta.Valor.Estado);

        var edicion = await _servicio.ActualizarAsync(publicId, Input("0002", "Prueba Editada", null));
        Assert.True(edicion.EsExito, string.Join("; ", edicion.Errores.Values));
        Assert.Equal(Prefijo + "0002", edicion.Valor.CiNit);
        Assert.Null(edicion.Valor.Correo);
        Assert.Equal(Admin, edicion.Valor.UsuarioModificacionId);
        Assert.NotNull(edicion.Valor.FechaModificacion);

        Assert.True((await _servicio.CambiarEstadoAsync(publicId, activo: false)).EsExito);
        var inactivo = await _servicio.ObtenerAsync(publicId);
        Assert.False(inactivo.Valor.Estado);
        Assert.Empty((await _servicio.BuscarParaVentaAsync(Prefijo)).Valor);

        // Reactivar dos veces: la segunda no cambia filas, pero el cliente existe.
        Assert.True((await _servicio.CambiarEstadoAsync(publicId, activo: true)).EsExito);
        Assert.True((await _servicio.CambiarEstadoAsync(publicId, activo: true)).EsExito);
        var encontrado = Assert.Single((await _servicio.BuscarParaVentaAsync(Prefijo)).Valor);
        Assert.Equal(publicId, encontrado.PublicId);
    }

    [MySqlFact]
    public async Task Ci_nit_repetido_es_conflicto_en_el_servicio()
    {
        Assert.True((await _servicio.RegistrarAsync(Input("0003", "Primero"))).EsExito);

        var repetido = await _servicio.RegistrarAsync(Input("0003", "Segundo"));

        Assert.Equal(TipoError.Conflicto, repetido.Tipo);
    }

    [MySqlFact]
    public async Task El_error_1062_de_mysql_se_traduce_a_ValorDuplicadoException()
    {
        await _repositorio.CrearAsync(Cliente("0004"));

        var error = await Assert.ThrowsAsync<ValorDuplicadoException>(() => _repositorio.CrearAsync(Cliente("0004")));

        Assert.Equal("CiNit", error.Campo);
        var mysql = Assert.IsType<MySqlException>(error.InnerException);
        Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, mysql.ErrorCode);
    }

    [MySqlFact]
    public async Task Buscar_para_venta_excluye_al_cliente_semilla_inactivo()
    {
        var resultado = await _servicio.BuscarParaVentaAsync("1234567");

        Assert.True(resultado.EsExito);
        Assert.Empty(resultado.Valor);
        Assert.Single((await _servicio.BuscarParaVentaAsync("9999999")).Valor);
    }

    [MySqlFact]
    public async Task Listar_filtra_ordena_y_trata_los_comodines_como_texto()
    {
        await _repositorio.CrearAsync(Cliente("0005", "Beta"));
        await _repositorio.CrearAsync(Cliente("0006", "Alfa"));

        var asc = await _servicio.ListarAsync(new FiltroClientes { Busqueda = Prefijo, Orden = OrdenClientes.RazonSocial });
        var desc = await _servicio.ListarAsync(new FiltroClientes
        {
            Busqueda = Prefijo, Orden = OrdenClientes.RazonSocial, Descendente = true
        });
        var comodin = await _servicio.ListarAsync(new FiltroClientes { Busqueda = "PRB114%" });
        var inactivos = await _servicio.ListarAsync(new FiltroClientes { Estado = false });

        Assert.Equal(new[] { "Alfa", "Beta" }, asc.Select(c => c.RazonSocial));
        Assert.Equal(new[] { "Beta", "Alfa" }, desc.Select(c => c.RazonSocial));
        Assert.Empty(comodin);
        Assert.Contains(inactivos, c => c.PublicId == ClienteSemillaInactivo);
        Assert.All(inactivos, c => Assert.False(c.Estado));
    }

    private static ClienteInput Input(string sufijo, string razonSocial, string? correo = null) =>
        new() { CiNit = Prefijo + sufijo, RazonSocial = razonSocial, Correo = correo };

    private static Cliente Cliente(string sufijo, string razonSocial = "Prueba") => new()
    {
        PublicId = Guid.NewGuid(),
        CiNit = Prefijo + sufijo,
        RazonSocial = razonSocial
    };

    private async Task BorrarDatosDePruebaAsync()
    {
        await using var connection = _conexiones.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Cliente WHERE CiNit LIKE 'PRB114-%'";
        await command.ExecuteNonQueryAsync();
    }

    private sealed class UsuarioFijo(Guid id) : IUsuarioActual
    {
        public Guid? UsuarioId => id;
        public string? NombreUsuario => "admin";
        public string? Iniciales => "A.S.";
    }
}
