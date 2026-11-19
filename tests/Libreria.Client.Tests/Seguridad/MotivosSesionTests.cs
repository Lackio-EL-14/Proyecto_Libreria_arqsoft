using System.Security.Claims;
using Libreria.Client.Seguridad;
using Xunit;

namespace Libreria.Client.Tests.Seguridad;

public sealed class MotivosSesionTests
{
    [Theory]
    [InlineData(MotivosSesion.Vencida)]
    [InlineData(MotivosSesion.Cerrada)]
    [InlineData(MotivosSesion.Inactiva)]
    [InlineData(MotivosSesion.NoDisponible)]
    [InlineData(MotivosSesion.Error)]
    public void CadaMotivo_TieneMensajeEnEspanol_YUnaUrlLocal(string motivo)
    {
        var mensaje = MotivosSesion.Mensaje(motivo);

        Assert.False(string.IsNullOrWhiteSpace(mensaje));
        Assert.EndsWith(".", mensaje);
        Assert.StartsWith("/Cuenta/Login?motivo=", MotivosSesion.UrlLogin(motivo));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<script>")]
    public void MotivoDesconocido_NoMuestraMensaje(string? motivo)
    {
        Assert.Null(MotivosSesion.Mensaje(motivo));
    }

    [Fact]
    public void ClaimsKeycloak_LeeSubNombreYRoles()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "a0000000-0000-0000-0000-000000000002"),
            new Claim("preferred_username", "vendedor"),
            new Claim("name", "Vendedor Mostrador"),
            new Claim("roles", "Vendedor")
        ], "prueba", "preferred_username", "roles"));

        Assert.Equal(Guid.Parse("a0000000-0000-0000-0000-000000000002"), principal.ObtenerSub());
        Assert.Equal("Vendedor Mostrador", principal.NombreVisible());
        Assert.Equal("Vendedor", principal.RolesVisibles());
        Assert.True(principal.IsInRole("Vendedor"));
        Assert.Equal("vendedor", principal.Identity!.Name);
    }

    [Fact]
    public void SinSubValido_DevuelveNull()
    {
        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "no-es-guid")])).ObtenerSub());
    }
}
