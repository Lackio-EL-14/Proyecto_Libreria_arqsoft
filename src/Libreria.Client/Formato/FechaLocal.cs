namespace Libreria.Client.Formato;

/// <summary>
/// Los microservicios guardan la auditoría en UTC (MySQL, SQL Server y MongoDB en contenedores UTC).
/// La UI la muestra en la hora de Bolivia (UTC−4, sin horario de verano).
/// </summary>
public static class FechaLocal
{
    private const string FormatoFechaHora = "dd/MM/yyyy HH:mm";

    private static readonly TimeZoneInfo ZonaBolivia = ObtenerZona();

    /// <summary>Convierte una fecha UTC (o sin zona, que se asume UTC) a la hora de Bolivia.</summary>
    public static DateTime ABolivia(DateTime fecha)
    {
        var utc = fecha.Kind == DateTimeKind.Local
            ? fecha.ToUniversalTime()
            : DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaBolivia);
    }

    /// <summary>«dd/MM/yyyy HH:mm» en hora de Bolivia.</summary>
    public static string Mostrar(DateTime fecha) => ABolivia(fecha).ToString(FormatoFechaHora);

    private static TimeZoneInfo ObtenerZona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
        }
    }
}
