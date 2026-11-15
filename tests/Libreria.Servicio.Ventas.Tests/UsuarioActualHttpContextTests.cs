using System.Security.Claims;
using Libreria.Servicio.Ventas.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Libreria.Servicio.Ventas.Tests;

public class UsuarioActualHttpContextTests
{
    [Fact]
    public void SinJwt_EnDesarrollo_UsaLaIdentidadConfigurada()
    {
        var publicId = Guid.NewGuid();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UsuarioDesarrollo:PublicId"] = publicId.ToString(),
                ["UsuarioDesarrollo:Nombre"] = "admin",
                ["UsuarioDesarrollo:Iniciales"] = "A.S."
            })
            .Build();
        var usuario = new UsuarioActualHttpContext(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            configuration,
            new EntornoFake("Development"));

        Assert.Equal(publicId, usuario.UsuarioId);
        Assert.Equal("admin", usuario.NombreUsuario);
        Assert.Equal("A.S.", usuario.Iniciales);
    }

    [Fact]
    public void ClaimsDelJwt_TienenPrioridadSobreElUsuarioDeDesarrollo()
    {
        var publicId = Guid.NewGuid();
        var contexto = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", publicId.ToString()),
                new Claim("name", "cesar"),
                new Claim("iniciales", "C.C.")
            ],
            "Bearer"))
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UsuarioDesarrollo:PublicId"] = Guid.NewGuid().ToString(),
                ["UsuarioDesarrollo:Nombre"] = "admin",
                ["UsuarioDesarrollo:Iniciales"] = "A.S."
            })
            .Build();
        var usuario = new UsuarioActualHttpContext(
            new HttpContextAccessor { HttpContext = contexto },
            configuration,
            new EntornoFake("Development"));

        Assert.Equal(publicId, usuario.UsuarioId);
        Assert.Equal("cesar", usuario.NombreUsuario);
        Assert.Equal("C.C.", usuario.Iniciales);
    }

    private sealed class EntornoFake(string nombre) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = nombre;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
