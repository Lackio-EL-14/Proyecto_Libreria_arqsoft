using Libreria.Servicio.Clientes.Domain;
using Libreria.Servicio.Clientes.Controllers.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Clientes.Tests.Comun;

public class ResultadoHttpExtensionsTests
{
    [Fact]
    public void Exito_sin_valor_devuelve_204()
    {
        var respuesta = Result.Exito().AActionResult();

        Assert.IsType<NoContentResult>(respuesta);
    }

    [Fact]
    public void Exito_con_valor_entrega_el_valor()
    {
        var respuesta = Result<int>.Exito(7).AActionResult(valor => new OkObjectResult(valor));

        var ok = Assert.IsType<OkObjectResult>(respuesta);
        Assert.Equal(7, ok.Value);
    }

    [Fact]
    public void Validacion_devuelve_400_con_errores_por_campo()
    {
        var respuesta = Result.Validacion("Nombre", "El nombre es obligatorio.").AActionResult();

        var objeto = Assert.IsType<ObjectResult>(respuesta);
        var problema = Assert.IsType<ValidationProblemDetails>(objeto.Value);
        Assert.Equal(400, objeto.StatusCode);
        Assert.Equal("El nombre es obligatorio.", problema.Errors["Nombre"].Single());
    }

    [Fact]
    public void NoEncontrado_devuelve_404()
    {
        var respuesta = Result.NoEncontrado("No existe.").AActionResult();

        var objeto = Assert.IsType<ObjectResult>(respuesta);
        var problema = Assert.IsType<ProblemDetails>(objeto.Value);
        Assert.Equal(404, objeto.StatusCode);
        Assert.Equal("No existe.", problema.Detail);
    }

    [Fact]
    public void Conflicto_devuelve_409()
    {
        var respuesta = Result<int>.Fallo(Result.Conflicto("CiNit", "Ya existe.")).AActionResult(_ => new OkResult());

        var objeto = Assert.IsType<ObjectResult>(respuesta);
        Assert.Equal(409, objeto.StatusCode);
        Assert.IsAssignableFrom<ProblemDetails>(objeto.Value);
    }
}
