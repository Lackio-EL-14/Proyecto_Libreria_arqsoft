using Libreria.Servicio.Usuarios.Domain;

namespace Libreria.Servicio.Usuarios.Tests.Domain;

/// <summary>Forma del Result pedida por el docente (US-139, contrato 6.5).</summary>
public class ResultDominioTests
{
    [Fact]
    public void Success_es_exito_y_no_tiene_errores()
    {
        var resultado = Result.Success();

        Assert.True(resultado.IsSuccess);
        Assert.False(resultado.IsError);
        Assert.Empty(resultado.Errors());
    }

    [Fact]
    public void Error_con_un_mensaje_devuelve_ese_mensaje()
    {
        var resultado = Result.Error("El nombre es obligatorio.");

        Assert.True(resultado.IsError);
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Equal(["El nombre es obligatorio."], resultado.Errors());
    }

    [Fact]
    public void Errors_devuelve_todos_los_errores_acumulados()
    {
        var resultado = Result.Validacion(new Dictionary<string, string>
        {
            ["Nombre"] = "El nombre es obligatorio.",
            ["CiNit"] = "El CI/NIT es obligatorio."
        });

        Assert.Equal(2, resultado.Errors().Length);
        Assert.Contains("El nombre es obligatorio.", resultado.Errors());
        Assert.Contains("El CI/NIT es obligatorio.", resultado.Errors());
    }

    [Fact]
    public void Error_sin_mensaje_es_un_error_de_programacion()
    {
        Assert.ThrowsAny<ArgumentException>(() => Result.Error(" "));
    }

    [Fact]
    public void Success_con_valor_expone_el_valor()
    {
        var resultado = Result<int>.Success(7);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(7, resultado.Valor);
        Assert.Empty(resultado.Errors());
    }

    [Fact]
    public void Error_generico_devuelve_el_mismo_tipo_de_objeto()
    {
        Result<int> resultado = Result<int>.Error("No válido.");

        Assert.True(resultado.IsError);
        Assert.Equal(["No válido."], resultado.Errors());
        Assert.Throws<InvalidOperationException>(() => resultado.Valor);
    }

    [Fact]
    public void IsError_siempre_es_lo_contrario_de_IsSuccess()
    {
        Result[] resultados =
        [
            Result.Success(),
            Result.Exito(),
            Result.Error("x"),
            Result.Validacion("Campo", "x"),
            Result.NoEncontrado("x"),
            Result.Conflicto("Campo", "x"),
            Result<int>.Success(1),
            Result<int>.Error("x"),
            Result<int>.Fallo(Result.NoEncontrado("x"))
        ];

        foreach (var resultado in resultados)
        {
            Assert.NotEqual(resultado.IsSuccess, resultado.IsError);
            Assert.Equal(resultado.IsSuccess, resultado.EsExito);
            Assert.Equal(resultado.IsSuccess, resultado.Errors().Length == 0);
        }
    }
}
