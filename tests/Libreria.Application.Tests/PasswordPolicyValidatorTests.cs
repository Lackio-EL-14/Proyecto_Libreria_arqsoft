using Libreria.Application.Validators;

namespace Libreria.Application.Tests;

public class PasswordPolicyValidatorTests
{
    private readonly PasswordPolicyValidator _validator = new();

    [Fact]
    public void Validar_ContrasenaValida_NoDevuelveErrores()
    {
        var errores = _validator.Validar("Libreria1!");

        Assert.Empty(errores);
    }

    [Fact]
    public void Validar_ExactamenteOchoCaracteres_NoDevuelveErrores()
    {
        var errores = _validator.Validar("Libro12!");

        Assert.Empty(errores);
    }

    [Fact]
    public void Validar_MenosDeOchoCaracteres_DevuelveMensajeDeLongitud()
    {
        var errores = _validator.Validar("Libro1!");

        Assert.Equal(
            "La contraseña debe tener al menos 8 caracteres.",
            Assert.Single(errores));
    }

    [Fact]
    public void Validar_SinMayusculas_DevuelveMensajeDeMayuscula()
    {
        var errores = _validator.Validar("libreria1!");

        Assert.Equal(
            "La contraseña debe contener al menos una letra mayúscula.",
            Assert.Single(errores));
    }

    [Fact]
    public void Validar_SinMinusculas_DevuelveMensajeDeMinuscula()
    {
        var errores = _validator.Validar("LIBRERIA1!");

        Assert.Equal(
            "La contraseña debe contener al menos una letra minúscula.",
            Assert.Single(errores));
    }

    [Fact]
    public void Validar_SinNumeros_DevuelveMensajeDeNumero()
    {
        var errores = _validator.Validar("Libreria!");

        Assert.Equal(
            "La contraseña debe contener al menos un número.",
            Assert.Single(errores));
    }

    [Fact]
    public void Validar_SinCaracteresEspeciales_DevuelveMensajeDeCaracterEspecial()
    {
        var errores = _validator.Validar("Libreria1");

        Assert.Equal(
            "La contraseña debe contener al menos un carácter especial.",
            Assert.Single(errores));
    }

    [Fact]
    public void Validar_ContrasenaVacia_DevuelveLosCincoMensajes()
    {
        var errores = _validator.Validar(string.Empty);

        Assert.Equal(
            new[]
            {
                "La contraseña debe tener al menos 8 caracteres.",
                "La contraseña debe contener al menos una letra mayúscula.",
                "La contraseña debe contener al menos una letra minúscula.",
                "La contraseña debe contener al menos un número.",
                "La contraseña debe contener al menos un carácter especial."
            },
            errores);
    }

    [Fact]
    public void Validar_ContrasenaNula_DevuelveLosCincoMensajes()
    {
        var errores = _validator.Validar(null);

        Assert.Equal(
            new[]
            {
                "La contraseña debe tener al menos 8 caracteres.",
                "La contraseña debe contener al menos una letra mayúscula.",
                "La contraseña debe contener al menos una letra minúscula.",
                "La contraseña debe contener al menos un número.",
                "La contraseña debe contener al menos un carácter especial."
            },
            errores);
    }

    [Fact]
    public void Validar_IncumpleVariasReglas_DevuelveTodosSusMensajes()
    {
        var errores = _validator.Validar("abc");

        Assert.Equal(
            new[]
            {
                "La contraseña debe tener al menos 8 caracteres.",
                "La contraseña debe contener al menos una letra mayúscula.",
                "La contraseña debe contener al menos un número.",
                "La contraseña debe contener al menos un carácter especial."
            },
            errores);
    }

    [Theory]
    [InlineData("Libreria1 ")]
    [InlineData("Libreria1\t")]
    [InlineData("Libreria1\n")]
    public void Validar_EspaciosNoSonCaracterEspecial_DevuelveMensajeDeCaracterEspecial(
        string contrasena)
    {
        var errores = _validator.Validar(contrasena);

        Assert.Equal(
            "La contraseña debe contener al menos un carácter especial.",
            Assert.Single(errores));
    }

    [Theory]
    [InlineData("Libreria1!")]
    [InlineData("Libreria1@")]
    [InlineData("Libreria1$")]
    [InlineData("Libreria1_")]
    public void Validar_ConPuntuacionOSimbolos_NoDevuelveErrores(
        string contrasena)
    {
        var errores = _validator.Validar(contrasena);

        Assert.Empty(errores);
    }
}
