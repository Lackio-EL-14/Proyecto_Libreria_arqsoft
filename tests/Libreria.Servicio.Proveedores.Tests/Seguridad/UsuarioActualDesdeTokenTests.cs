using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Libreria.Servicio.Proveedores.Infrastructure.Seguridad;
namespace Libreria.Servicio.Proveedores.Tests.Seguridad;
public class UsuarioActualDesdeTokenTests
{
    [Theory]
    [InlineData(true, "a0000000-0000-0000-0000-000000000002", "vendedor", "a0000000-0000-0000-0000-000000000002")]
    [InlineData(false, "a0000000-0000-0000-0000-000000000001", "admin", null)]
    [InlineData(true, "invalido", "admin", null)]
    [InlineData(true, "00000000-0000-0000-0000-000000000000", "admin", null)]
    public void LeeSoloIdentidadAutenticada(bool autenticado, string sub, string nombre, string? esperado)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", sub), new Claim("preferred_username", nombre) }, autenticado ? "Bearer" : null));
        context.Request.Headers["X-Usuario-Id"] = Guid.NewGuid().ToString();
        var actor = new UsuarioActualDesdeToken(new HttpContextAccessor { HttpContext = context });
        Assert.Equal(esperado is null ? (Guid?)null : Guid.Parse(esperado), actor.UsuarioId);
        Assert.Equal(autenticado ? nombre : null, actor.NombreUsuario);
    }
    [Fact]
    public void SinContextoNoInventaAdministrador()
    {
        var actor = new UsuarioActualDesdeToken(new HttpContextAccessor());
        Assert.Null(actor.UsuarioId);
        Assert.Null(actor.NombreUsuario);
        Assert.Null(actor.Iniciales);
    }
    [Fact]
    public void ConsultaLaIdentidadActualEnCadaPeticion()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var actor = new UsuarioActualDesdeToken(accessor);
        foreach (var nombre in new[] { "admin", "vendedor" })
        {
            var id = Guid.NewGuid();
            accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", id.ToString()), new Claim("preferred_username", nombre) }, "Bearer"));
            Assert.Equal(id, actor.UsuarioId);
            Assert.Equal(nombre, actor.NombreUsuario);
        }
    }
}