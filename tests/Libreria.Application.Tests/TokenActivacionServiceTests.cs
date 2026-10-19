using Libreria.Application.Services;

namespace Libreria.Application.Tests;

public class TokenActivacionServiceTests
{
    [Fact]
    public void GenerarToken_GeneraTokensDistintos()
    {
        var token1 = TokenActivacionService.GenerarToken();
        var token2 = TokenActivacionService.GenerarToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerarToken_GeneraTokenSeguroParaUrl()
    {
        var token = TokenActivacionService.GenerarToken();

        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void CalcularHash_MismoTokenProduceMismoHash()
    {
        const string token = "token-prueba";

        var hash1 = TokenActivacionService.CalcularHash(token);
        var hash2 = TokenActivacionService.CalcularHash(token);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void CalcularHash_NoGuardaElTokenEnClaro()
    {
        const string token = "token-prueba";

        var hash = TokenActivacionService.CalcularHash(token);

        Assert.NotEqual(token, hash);
        Assert.Equal(64, hash.Length);
    }
}