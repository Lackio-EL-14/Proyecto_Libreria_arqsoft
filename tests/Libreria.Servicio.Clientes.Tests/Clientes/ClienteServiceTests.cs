using Libreria.Servicio.Clientes.Application.Comun;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Services;
using Libreria.Servicio.Clientes.Application.Validators;
using Libreria.Servicio.Clientes.Tests.Fakes;

namespace Libreria.Servicio.Clientes.Tests.Clientes;

public class ClienteServiceTests
{
    private readonly ClienteRepositoryFalso _repositorio = new();
    private readonly ClienteRepositoryFactoryFalsa _fabrica;
    private readonly ClienteService _servicio;

    public ClienteServiceTests()
    {
        _fabrica = new ClienteRepositoryFactoryFalsa(_repositorio);
        _servicio = new ClienteService(_fabrica, new ClienteValidator(_fabrica));
    }

    [Fact]
    public void El_servicio_obtiene_su_repositorio_de_la_fabrica()
    {
        Assert.True(_fabrica.VecesUsada >= 1);
    }

    [Fact]
    public async Task Registrar_datos_validos_crea_el_cliente_activo_con_auditoria()
    {
        var resultado = await _servicio.RegistrarAsync(Input("  9876543 ", " Ana   Rojas ", "ana@correo.bo"));

        Assert.True(resultado.EsExito);
        var cliente = resultado.Valor;
        Assert.NotEqual(Guid.Empty, cliente.PublicId);
        Assert.Equal("9876543", cliente.CiNit);
        Assert.Equal("Ana Rojas", cliente.RazonSocial);
        Assert.True(cliente.Estado);
        Assert.Equal(ClienteRepositoryFalso.UsuarioPrueba, cliente.UsuarioCreacionId);
        Assert.Single(_repositorio.Clientes);
    }

    [Fact]
    public async Task Registrar_sin_ci_nit_es_validacion_y_no_guarda()
    {
        var resultado = await _servicio.RegistrarAsync(Input("  ", "Ana"));

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.True(resultado.Errores.ContainsKey("CiNit"));
        Assert.Empty(_repositorio.Clientes);
    }

    [Fact]
    public async Task Registrar_ci_nit_existente_es_conflicto()
    {
        _repositorio.Agregar("9876543", "Otro");

        var resultado = await _servicio.RegistrarAsync(Input("9876543", "Ana"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(ClienteValidator.MensajeCiNitDuplicado, resultado.Errores["CiNit"]);
        Assert.Single(_repositorio.Clientes);
    }

    [Fact]
    public async Task Registrar_cuando_la_bd_rechaza_el_duplicado_es_conflicto()
    {
        _repositorio.SimularDuplicadoEnEscritura = true;

        var resultado = await _servicio.RegistrarAsync(Input("9876543", "Ana"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(ClienteValidator.MensajeCiNitDuplicado, resultado.Errores["CiNit"]);
    }

    [Fact]
    public async Task Actualizar_cambia_los_datos_y_registra_quien_modifico()
    {
        var cliente = _repositorio.Agregar("111", "Ana");

        var resultado = await _servicio.ActualizarAsync(cliente.PublicId, Input("222", "Ana María", "ana@correo.bo"));

        Assert.True(resultado.EsExito);
        Assert.Equal("222", resultado.Valor.CiNit);
        Assert.Equal("Ana María", resultado.Valor.RazonSocial);
        Assert.Equal(ClienteRepositoryFalso.UsuarioPrueba, resultado.Valor.UsuarioModificacionId);
    }

    [Fact]
    public async Task Actualizar_manteniendo_su_propio_ci_nit_es_exito()
    {
        var cliente = _repositorio.Agregar("111", "Ana");

        var resultado = await _servicio.ActualizarAsync(cliente.PublicId, Input("111", "Ana María"));

        Assert.True(resultado.EsExito);
    }

    [Fact]
    public async Task Actualizar_con_el_ci_nit_de_otro_es_conflicto()
    {
        _repositorio.Agregar("111", "Ana");
        var otro = _repositorio.Agregar("222", "Luis");

        var resultado = await _servicio.ActualizarAsync(otro.PublicId, Input("111", "Luis"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal("222", _repositorio.Clientes.Single(c => c.PublicId == otro.PublicId).CiNit);
    }

    [Fact]
    public async Task Actualizar_inexistente_es_no_encontrado()
    {
        var resultado = await _servicio.ActualizarAsync(Guid.NewGuid(), Input("111", "Ana"));

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task Actualizar_un_inactivo_es_conflicto()
    {
        var cliente = _repositorio.Agregar("111", "Ana", estado: false);

        var resultado = await _servicio.ActualizarAsync(cliente.PublicId, Input("111", "Ana María"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(ClienteService.MensajeInactivo, resultado.Errores[string.Empty]);
    }

    [Fact]
    public async Task Actualizar_con_datos_invalidos_es_validacion()
    {
        var cliente = _repositorio.Agregar("111", "Ana");

        var resultado = await _servicio.ActualizarAsync(cliente.PublicId, Input("111", "Ana", "no-es-correo"));

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.True(resultado.Errores.ContainsKey("Correo"));
    }

    [Fact]
    public async Task Cambiar_estado_da_de_baja_y_reactiva_sin_borrar()
    {
        var cliente = _repositorio.Agregar("111", "Ana");

        var baja = await _servicio.CambiarEstadoAsync(cliente.PublicId, activo: false);
        Assert.True(baja.EsExito);
        Assert.False(_repositorio.Clientes.Single().Estado);

        var alta = await _servicio.CambiarEstadoAsync(cliente.PublicId, activo: true);
        Assert.True(alta.EsExito);
        Assert.True(_repositorio.Clientes.Single().Estado);
    }

    [Fact]
    public async Task Cambiar_estado_de_inexistente_es_no_encontrado()
    {
        var resultado = await _servicio.CambiarEstadoAsync(Guid.NewGuid(), activo: false);

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task Buscar_para_venta_no_devuelve_inactivos_y_expone_el_public_id()
    {
        var activo = _repositorio.Agregar("1234", "Ana");
        _repositorio.Agregar("12345", "Luis", estado: false);

        var resultado = await _servicio.BuscarParaVentaAsync(" 123 ");

        Assert.True(resultado.EsExito);
        var encontrado = Assert.Single(resultado.Valor);
        Assert.Equal(new ClienteVentaItem(activo.PublicId, "1234", "Ana"), encontrado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Buscar_para_venta_sin_ci_nit_es_validacion(string? ciNit)
    {
        var resultado = await _servicio.BuscarParaVentaAsync(ciNit);

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
    }

    [Fact]
    public async Task Obtener_inexistente_es_no_encontrado()
    {
        var resultado = await _servicio.ObtenerAsync(Guid.NewGuid());

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task Listar_respeta_el_filtro_de_estado()
    {
        _repositorio.Agregar("1", "Ana");
        _repositorio.Agregar("2", "Luis", estado: false);

        var activos = await _servicio.ListarAsync(new FiltroClientes());
        var todos = await _servicio.ListarAsync(new FiltroClientes { Estado = null });

        Assert.Single(activos);
        Assert.Equal(2, todos.Count);
    }

    private static ClienteInput Input(string ciNit, string razonSocial, string? correo = null) =>
        new() { CiNit = ciNit, RazonSocial = razonSocial, Correo = correo };
}
