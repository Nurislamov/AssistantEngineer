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
        double heatRecoveryEfficiency,
        double infiltrationAirChangesPerHour,
        double windExposureFactor,
        double stackCoefficient,
        double windCoefficient)
    {
        var validation = Validate(
            airChangesPerHour,
            heatRecoveryEfficiency,
            infiltrationAirChangesPerHour,
            windExposureFactor,
            stackCoefficient,
            windCoefficient);

        if (validation.IsFailure)
            return Result<VentilationParameters>.Failure(validation);

        return Result<VentilationParameters>.Success(new VentilationParameters(
            airChangesPerHour,
            heatRecoveryEfficiency,
            infiltrationAirChangesPerHour,
            windExposureFactor,
            stackCoefficient,
            windCoefficient));
    }

    public static Result<VentilationParameters> Create(
        double airChangesPerHour,
        double heatRecoveryEfficiency = 0) =>
        Create(
            airChangesPerHour,
            heatRecoveryEfficiency,
            infiltrationAirChangesPerHour: 0,
            windExposureFactor: 0,
            stackCoefficient: 0,
            windCoefficient: 0);

    public Result Update(
        double airChangesPerHour,
        double heatRecoveryEfficiency,
        double infiltrationAirChangesPerHour,
        double windExposureFactor,
        double stackCoefficient,
        double windCoefficient)
    {
        var validation = Validate(
            airChangesPerHour,
            heatRecoveryEfficiency,
            infiltrationAirChangesPerHour,
            windExposureFactor,
            stackCoefficient,
            windCoefficient);

        if (validation.IsFailure)
            return validation;

        AirChangesPerHour = airChangesPerHour;
        HeatRecoveryEfficiency = heatRecoveryEfficiency;
        InfiltrationAirChangesPerHour = infiltrationAirChangesPerHour;
        WindExposureFactor = windExposureFactor;
        StackCoefficient = stackCoefficient;
        WindCoefficient = windCoefficient;

        return Result.Success();
    }

    private static Result Validate(
        double airChangesPerHour,
        double heatRecoveryEfficiency,
        double infiltrationAirChangesPerHour,
        double windExposureFactor,
        double stackCoefficient,
        double windCoefficient)
    {
        if (airChangesPerHour < 0)
            return Result.Validation("Air changes per hour cannot be negative.");

        if (heatRecoveryEfficiency is < 0 or > 1)
            return Result.Validation("Heat recovery efficiency must be between 0 and 1.");

        if (infiltrationAirChangesPerHour < 0)
            return Result.Validation("Infiltration air changes per hour cannot be negative.");

        if (windExposureFactor is < 0 or > 5)
            return Result.Validation("Wind exposure factor must be between 0 and 5.");

        if (stackCoefficient is < 0 or > 5)
            return Result.Validation("Stack coefficient must be between 0 and 5.");

        if (windCoefficient is < 0 or > 5)
            return Result.Validation("Wind coefficient must be between 0 and 5.");

        return Result.Success();
    }
}
