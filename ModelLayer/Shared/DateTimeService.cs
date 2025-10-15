namespace ModelLayer.Shared;

/// <summary>
/// Servicio para manejo de fechas en zona horaria de Costa Rica (UTC-6)
/// </summary>
public class DateTimeService
{
    private static readonly TimeZoneInfo CostaRicaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    /// <summary>
    /// Obtiene la fecha y hora actual en zona horaria de Costa Rica
    /// </summary>
    /// <returns>DateTime en zona horaria de Costa Rica</returns>
    public static DateTime GetCostaRicaNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CostaRicaTimeZone);
    }

    /// <summary>
    /// Convierte una fecha UTC a zona horaria de Costa Rica
    /// </summary>
    /// <param name="utcDateTime">Fecha en UTC</param>
    /// <returns>DateTime en zona horaria de Costa Rica</returns>
    public static DateTime ConvertFromUtcToCostaRica(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CostaRicaTimeZone);
    }

    /// <summary>
    /// Convierte una fecha de Costa Rica a UTC
    /// </summary>
    /// <param name="costaRicaDateTime">Fecha en zona horaria de Costa Rica</param>
    /// <returns>DateTime en UTC</returns>
    public static DateTime ConvertFromCostaRicaToUtc(DateTime costaRicaDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(costaRicaDateTime, CostaRicaTimeZone);
    }

    /// <summary>
    /// Obtiene la fecha actual en zona horaria de Costa Rica (solo fecha, sin hora)
    /// </summary>
    /// <returns>DateOnly en zona horaria de Costa Rica</returns>
    public static DateOnly GetCostaRicaToday()
    {
        var now = GetCostaRicaNow();
        return DateOnly.FromDateTime(now);
    }
}
