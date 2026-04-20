using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Domain.ValueObjects;

public record SolarHeatGainCoefficient
{
    public double Value { get; }

    private SolarHeatGainCoefficient(double value) => Value = value;

    public static Result<SolarHeatGainCoefficient> FromValue(double value)
    {
        var finiteCheck = Guard.AgainstNonFinite(value, "SHGC");
        if (finiteCheck.IsFailure) return Result<SolarHeatGainCoefficient>.Failure(finiteCheck);

        if (value is < 0 or > 1)
            return Result<SolarHeatGainCoefficient>.Validation("SHGC must be between 0 and 1.");

        return Result<SolarHeatGainCoefficient>.Success(new SolarHeatGainCoefficient(value));
    }

    public override string ToString() => Value.ToString("F2");
}
