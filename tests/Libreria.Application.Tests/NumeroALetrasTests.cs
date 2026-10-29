using Libreria.Servicio.Ventas.Application.Services;

namespace Libreria.Application.Tests;

public class NumeroALetrasTests
{
    [Theory]
    [InlineData(0, "Son: cero con 00/100 bolivianos")]
    [InlineData(1, "Son: uno con 00/100 bolivianos")]
    [InlineData(21, "Son: veintiuno con 00/100 bolivianos")]
    [InlineData(100, "Son: cien con 00/100 bolivianos")]
    [InlineData(101, "Son: ciento uno con 00/100 bolivianos")]
    [InlineData(1000, "Son: mil con 00/100 bolivianos")]
    [InlineData(1000000, "Son: un millón con 00/100 bolivianos")]
    [InlineData(2000000, "Son: dos millones con 00/100 bolivianos")]
    public void Convertir_DebeConvertirCasosPrincipales(
        int monto,
        string esperado)
    {
        var resultado = NumeroALetras.Convertir(monto);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void Convertir_DebeConvertirMontoConCentavos()
    {
        var resultado = NumeroALetras.Convertir(1250.75m);

        Assert.Equal(
            "Son: mil doscientos cincuenta con 75/100 bolivianos",
            resultado);
    }

    [Fact]
    public void Convertir_DebeMantenerCentavosConCeroInicial()
    {
        var resultado = NumeroALetras.Convertir(10.05m);

        Assert.Equal(
            "Son: diez con 05/100 bolivianos",
            resultado);
    }

    [Fact]
    public void Convertir_DebeRedondearADosDecimales()
    {
        var resultado = NumeroALetras.Convertir(10.999m);

        Assert.Equal(
            "Son: once con 00/100 bolivianos",
            resultado);
    }

    [Fact]
    public void Convertir_DebeRechazarMontosNegativos()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NumeroALetras.Convertir(-1m));
    }
}
