using Libreria.Servicio.Usuarios.Application.Services;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class TemporaryPasswordGeneratorTests
{
    private readonly TemporaryPasswordGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsPasswordWithExactLengthOfTwelve()
    {
        var password = _generator.Generate();

        Assert.Equal(12, password.Length);
    }

    [Fact]
    public void Generate_SatisfiesAllRequiredCharacterCategories()
    {
        var password = _generator.Generate();

        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, c => "!@#$%*-_+".Contains(c));
    }

    [Fact]
    public void Generate_MultipleCalls_ProduceDifferentPasswords()
    {
        var first = _generator.Generate();
        var second = _generator.Generate();

        Assert.NotEqual(first, second);
    }
}
