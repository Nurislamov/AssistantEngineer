using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.SharedKernel.ValueObjects;

public record ThermalTransmittance
{
    public double Value { get; }

    private ThermalTransmittance(double value) => Value = value;

    public static Result<ThermalTransmittance> FromValue(double value)
    {
        var finiteCheck = Guard.AgainstNonFinite(value, "U-Value");
        if (finiteCheck.IsFailure) return Result<ThermalTransmittance>.Failure(finiteCheck);

        if (value <= 0)
            return Result<ThermalTransmittance>.Validation("U-Value must be positive.");

        return Result<ThermalTransmittance>.Success(new ThermalTransmittance(value));
    }

    public override string ToString() => $"{Value:F3} W/(m2*K)";
}
