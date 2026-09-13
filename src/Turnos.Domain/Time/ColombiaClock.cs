namespace Turnos.Domain.Time;

public static class ColombiaClock
{
    private static readonly TimeZoneInfo ZonaHoraria = ObtenerZonaHoraria();

    public static DateTime Ahora =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHoraria);

    private static TimeZoneInfo ObtenerZonaHoraria()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }
}
