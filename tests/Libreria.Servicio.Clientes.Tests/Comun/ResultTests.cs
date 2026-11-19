using Libreria.Servicio.Clientes.Domain;

namespace Libreria.Servicio.Clientes.Tests.Comun;

public class ResultTests
{
    [Fact]
    public void Exito_no_tiene_errores()
    {
        var resultado = Result.Exito();

        Assert.True(resultado.EsExito);
        Assert.Equal(TipoError.Ninguno, resultado.Tipo);
        Assert.Empty(resultado.Errores);
    }

    [Fact]
    public void Validacion_guarda_el_campo_y_el_mensaje()
    {
        var resultado = Result.Validacion("Nombre", "El nombre es obligatorio.");

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Equal("El nombre es obligatorio.", resultado.Errores["Nombre"]);
    }

    [Fact]
    public void Validacion_sin_errores_es_un_error_de_programacion()
    {
        Assert.Throws<ArgumentException>(() => Result.Validacion(new Dictionary<string, string>()));
    }

    [Fact]
    public void Exito_con_valor_expone_el_valor()
    {
        Result<string> resultado = "hola";

        Assert.True(resultado.EsExito);
        Assert.Equal("hola", resultado.Valor);
    }

    [Fact]
    public void Fallo_conserva_el_tipo_y_no_tiene_valor()
    {
        var resultado = Result<string>.Fallo(Result.NoEncontrado("No existe."));

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        Assert.Throws<InvalidOperationException>(() => resultado.Valor);
    }

    [Fact]
    public void Fallo_no_acepta_un_resultado_exitoso()
    {
        Assert.Throws<ArgumentException>(() => Result<string>.Fallo(Result.Exito()));
    }
}
