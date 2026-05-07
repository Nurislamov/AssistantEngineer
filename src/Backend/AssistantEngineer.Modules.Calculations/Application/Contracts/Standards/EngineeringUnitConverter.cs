namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

public static class EngineeringUnitConverter
{
    private const double JoulesPerKiloWattHour = 3_600_000.0;

    public static double KilowattsToWatts(double kilowatts) => kilowatts * 1000.0;

    public static double WattsToKilowatts(double watts) => watts / 1000.0;

    public static double KilowattHoursToWattHours(double kilowattHours) => kilowattHours * 1000.0;

    public static double WattHoursToKilowattHours(double wattHours) => wattHours / 1000.0;

    public static double MegaJoulesToKilowattHours(double megaJoules) =>
        megaJoules * 1_000_000.0 / JoulesPerKiloWattHour;

    public static double KilowattHoursToMegaJoules(double kilowattHours) =>
        kilowattHours * JoulesPerKiloWattHour / 1_000_000.0;

    public static double CelsiusDeltaToKelvin(double celsiusDelta) => celsiusDelta;

    public static double KelvinDeltaToCelsius(double kelvinDelta) => kelvinDelta;

    public static double CubicMetersPerHourToKilogramsPerSecond(
        double cubicMetersPerHour,
        double airDensityKgPerM3)
    {
        EnsureNonNegative(cubicMetersPerHour, nameof(cubicMetersPerHour));
        EnsurePositive(airDensityKgPerM3, nameof(airDensityKgPerM3));
        return cubicMetersPerHour * airDensityKgPerM3 / 3600.0;
    }

    public static double KilogramsPerSecondToCubicMetersPerHour(
        double kilogramsPerSecond,
        double airDensityKgPerM3)
    {
        EnsureNonNegative(kilogramsPerSecond, nameof(kilogramsPerSecond));
        EnsurePositive(airDensityKgPerM3, nameof(airDensityKgPerM3));
        return kilogramsPerSecond / airDensityKgPerM3 * 3600.0;
    }

    public static double AirChangesPerHourToCubicMetersPerHour(
        double airChangesPerHour,
        double volumeCubicMeters)
    {
        EnsureNonNegative(airChangesPerHour, nameof(airChangesPerHour));
        EnsureNonNegative(volumeCubicMeters, nameof(volumeCubicMeters));
        return airChangesPerHour * volumeCubicMeters;
    }

    public static double CubicMetersPerHourToAirChangesPerHour(
        double cubicMetersPerHour,
        double volumeCubicMeters)
    {
        EnsureNonNegative(cubicMetersPerHour, nameof(cubicMetersPerHour));
        EnsurePositive(volumeCubicMeters, nameof(volumeCubicMeters));
        return cubicMetersPerHour / volumeCubicMeters;
    }

    private static void EnsureNonNegative(double value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
    }

    private static void EnsurePositive(double value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
    }
}
