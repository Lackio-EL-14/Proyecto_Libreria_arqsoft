using Libreria.Servicio.Clientes.Domain;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Validators;
using Libreria.Servicio.Clientes.Tests.Fakes;

namespace Libreria.Servicio.Clientes.Tests.Clientes;

public class ClienteValidatorTests
{
    private readonly ClienteRepositoryFalso _repositorio = new();
    private readonly ClienteValidator _validador;

    public ClienteValidatorTests()
    {
        _validador = new ClienteValidator(new ClienteRepositoryFactoryFalsa(_repositorio));
    }

    [Fact]
    public void Normalizar_recorta_y_compacta_espacios_y_vacia_el_correo_en_blanco()
    {
        var input = new ClienteInput { CiNit = "  123 ", RazonSocial = "  Juan   Pérez  ", Correo = "   " };

        _validador.Normalizar(input);

        Assert.Equal("123", input.CiNit);
        Assert.Equal("Juan Pérez", input.RazonSocial);
        Assert.Null(input.Correo);
    }

    [Fact]
    public async Task Datos_correctos_son_exito()
    {
        var resultado = await _validador.ValidarAsync(Input("123", "Juan", "juan@correo.bo"));

        Assert.True(resultado.EsExito);
    }

    [Fact]
    public async Task CiNit_y_razon_social_vacios_son_validacion_con_ambos_campos()
    {
        var resultado = await _validador.ValidarAsync(Input("", ""));

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Equal("El CI/NIT es obligatorio.", resultado.Errores["CiNit"]);
        Assert.Equal("La razón social es obligatoria.", resultado.Errores["RazonSocial"]);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@")]
    [InlineData("Nombre <a@b.bo>")]
    public async Task Correo_con_formato_invalido_es_validacion(string correo)
    {
        var resultado = await _validador.ValidarAsync(Input("123", "Juan", correo));

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Equal("Ingrese un correo electrónico válido.", resultado.Errores["Correo"]);
    }

    [Fact]
    public async Task Largos_maximos_se_validan()
    {
        var resultado = await _validador.ValidarAsync(
            Input(new string('1', 31), new string('a', 201), new string('a', 250) + "@b.bo"));

        Assert.Equal("El CI/NIT no puede superar los 30 caracteres.", resultado.Errores["CiNit"]);
        Assert.Equal("La razón social no puede superar los 200 caracteres.", resultado.Errores["RazonSocial"]);
        Assert.Equal("El correo no puede superar los 254 caracteres.", resultado.Errores["Correo"]);
    }

    [Fact]
    public async Task CiNit_repetido_es_conflicto_aunque_el_otro_este_inactivo()
    {
        _repositorio.Agregar("123", "Otro", estado: false);

        var resultado = await _validador.ValidarAsync(Input("123", "Juan"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(ClienteValidator.MensajeCiNitDuplicado, resultado.Errores["CiNit"]);
    }

    [Fact]
    public async Task En_la_edicion_el_cliente_no_choca_consigo_mismo()
    {
        var propio = _repositorio.Agregar("123", "Juan");

        var resultado = await _validador.ValidarAsync(Input("123", "Juan Carlos"), propio.PublicId);

        Assert.True(resultado.EsExito);
    }

    [Fact]
    public async Task Con_errores_de_formato_no_consulta_la_unicidad()
    {
        _repositorio.Agregar("123", "Otro");

        var resultado = await _validador.ValidarAsync(Input("123", ""));

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.False(resultado.Errores.ContainsKey("CiNit"));
    }

    private static ClienteInput Input(string ciNit, string razonSocial, string? correo = null) =>
        new() { CiNit = ciNit, RazonSocial = razonSocial, Correo = correo };
}
