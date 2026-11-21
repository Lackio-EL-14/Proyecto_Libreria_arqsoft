using Libreria.Client.Formato;
using Xunit;

namespace Libreria.Client.Tests.Formato;

/// <summary>Auditoría 20/11: la bitácora mostraba la hora UTC como si fuera la de Bolivia.</summary>
public sealed class FechaLocalTests
{
    [Fact]
    public void Una_fecha_utc_se_muestra_en_hora_de_Bolivia()
    {
        var utc = new DateTime(2026, 11, 21, 3, 12, 0, DateTimeKind.Utc);

        Assert.Equal("20/11/2026 23:12", FechaLocal.Mostrar(utc));
    }

    [Fact]
    public void Una_fecha_sin_zona_se_asume_utc()
    {
        var sinZona = new DateTime(2026, 11, 21, 3, 12, 0, DateTimeKind.Unspecified);

        Assert.Equal("20/11/2026 23:12", FechaLocal.Mostrar(sinZona));
    }
}
