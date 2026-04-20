using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.Climate;

namespace AssistantEngineer.Application.Services.Calculations.Iso52016;

public interface ISolarRadiationService
{
    /// <summary>
    /// Пересчитывает прямую и рассеянную радиацию из горизонтальной плоскости на вертикальную поверхность заданной ориентации.
    /// </summary>
    /// <param name="hourlyData">Данные за час (прямая и рассеянная на горизонтальную поверхность).</param>
    /// <param name="orientation">Ориентация поверхности.</param>
    /// <param name="latitude">Широта местности (для расчёта углов солнца).</param>
    /// <param name="dayOfYear">День года (1..365).</param>
    /// <param name="hour">Час суток (0..23).</param>
    /// <returns>Суммарная солнечная радиация на поверхность (Вт/м²).</returns>
    double CalculateVerticalSurfaceRadiation(
        HourlyClimateData hourlyData,
        CardinalDirection orientation,
        double latitude,
        int dayOfYear,
        int hour);
}

public class SolarRadiationService : ISolarRadiationService
{
    public double CalculateVerticalSurfaceRadiation(
        HourlyClimateData hourlyData,
        CardinalDirection orientation,
        double latitude,
        int dayOfYear,
        int hour)
    {
        // Углы солнца (упрощённо)
        var solarDeclination = 23.45 * Math.Sin(2 * Math.PI * (284 + dayOfYear) / 365.0);
        var solarTime = hour + 0.5; // середина часа
        var hourAngle = 15.0 * (solarTime - 12);

        var latRad = latitude * Math.PI / 180.0;
        var declRad = solarDeclination * Math.PI / 180.0;
        var hourRad = hourAngle * Math.PI / 180.0;

        var sinAltitude = Math.Sin(latRad) * Math.Sin(declRad) + Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(hourRad);
        var altitude = Math.Asin(Math.Max(0, sinAltitude)) * 180.0 / Math.PI;

        if (altitude <= 0)
            return 0.0;

        var cosZenith = Math.Max(0, sinAltitude);
        var azimuthArgument = (Math.Sin(declRad) - Math.Sin(latRad) * sinAltitude) /
            Math.Max(Math.Cos(latRad) * Math.Cos(Math.Asin(sinAltitude)), 0.0001);
        var azimuthRad = Math.Acos(Math.Clamp(azimuthArgument, -1, 1));
        var azimuth = azimuthRad * 180.0 / Math.PI;

        // Коррекция азимута по времени
        if (solarTime > 12)
            azimuth = 360 - azimuth;

        // Угол падения на вертикальную поверхность
        double surfaceAzimuth = GetSurfaceAzimuth(orientation);
        double incidentAngle = Math.Abs(azimuth - surfaceAzimuth);
        if (incidentAngle > 180)
            incidentAngle = 360 - incidentAngle;

        // Прямая составляющая
        double directOnSurface = hourlyData.DirectSolarRadiation *
            Math.Max(0, Math.Cos(incidentAngle * Math.PI / 180.0));

        // Рассеянная (изотропная модель)
        double diffuseOnSurface = hourlyData.DiffuseSolarRadiation * (1 + Math.Cos(90 * Math.PI / 180.0)) / 2; // для вертикальной поверхности 90°

        // Отражённая от земли (альбедо 0.2)
        double groundReflected = 0.2 * (hourlyData.DirectSolarRadiation * sinAltitude + hourlyData.DiffuseSolarRadiation) * (1 - Math.Cos(90 * Math.PI / 180.0)) / 2;

        return Math.Max(0, directOnSurface + diffuseOnSurface + groundReflected);
    }

    private double GetSurfaceAzimuth(CardinalDirection orientation) => orientation switch
    {
        CardinalDirection.North => 0,
        CardinalDirection.NorthEast => 45,
        CardinalDirection.East => 90,
        CardinalDirection.SouthEast => 135,
        CardinalDirection.South => 180,
        CardinalDirection.SouthWest => 225,
        CardinalDirection.West => 270,
        CardinalDirection.NorthWest => 315,
        _ => 0
    };
}
