using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Domain.Ventilation;

public class VentilationParameters
{
    public int Id { get; private set; }
    public double AirChangesPerHour { get; private set; }
    public double HeatRecoveryEfficiency { get; private set; }
    public double InfiltrationAirChangesPerHour { get; private set; }
    public double WindExposureFactor { get; private set; }
    public double StackCoefficient { get; private set; }
    public double WindCoefficient { get; private set; }

    private VentilationParameters() { }

    private VentilationParameters(
        double airChangesPerHour,
        double heatRecoveryEfficiency,
        double infiltrationAirChangesPerHour,
        double windExposureFactor,
        double stackCoefficient,
        double windCoefficient)
    {
        AirChangesPerHour = airChangesPerHour;
        HeatRecoveryEfficiency = heatRecoveryEfficiency;
        InfiltrationAirChangesPerHour = infiltrationAirChangesPerHour;
        WindExposureFactor = windExposureFactor;
        StackCoefficient = stackCoefficient;
        WindCoefficient = windCoefficient;
    }

    public static Result<VentilationParameters> Create(
        double airChangesPerHour,
        double heatRecoveryEfficiency = 0,
        double infiltrationAirChangesPerHour = 0,
        double windExposureFactor = 1,
        double stackCoefficient = 0,
        double windCoefficient = 0)
    {
        var airChangeCheck = Guard.AgainstNegative(airChangesPerHour, "Air changes per hour");
        if (airChangeCheck.IsFailure) return Result<VentilationParameters>.Failure(airChangeCheck);

        var recoveryCheck = Guard.AgainstRange(heatRecoveryEfficiency, 0, 1, "Heat recovery efficiency");
        if (recoveryCheck.IsFailure) return Result<VentilationParameters>.Failure(recoveryCheck);

        var infiltrationCheck = Guard.AgainstNegative(infiltrationAirChangesPerHour, "Infiltration air changes per hour");
        if (infiltrationCheck.IsFailure) return Result<VentilationParameters>.Failure(infiltrationCheck);

        var exposureCheck = Guard.AgainstRange(windExposureFactor, 0, 5, "Wind exposure factor");
        if (exposureCheck.IsFailure) return Result<VentilationParameters>.Failure(exposureCheck);

        var stackCheck = Guard.AgainstRange(stackCoefficient, 0, 1, "Stack coefficient");
        if (stackCheck.IsFailure) return Result<VentilationParameters>.Failure(stackCheck);

        var windCheck = Guard.AgainstRange(windCoefficient, 0, 1, "Wind coefficient");
        if (windCheck.IsFailure) return Result<VentilationParameters>.Failure(windCheck);

        return Result<VentilationParameters>.Success(new VentilationParameters(
            airChangesPerHour,
            heatRecoveryEfficiency,
            infiltrationAirChangesPerHour,
            windExposureFactor,
            stackCoefficient,
            windCoefficient));
    }
}
