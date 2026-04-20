using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Domain.ValueObjects;

public record Power
{
    public double Watts { get; }

    private Power(double watts) => Watts = watts;

    public static Result<Power> FromWatts(double value)
    {
        var finiteCheck = Guard.AgainstNonFinite(value, "Power");
        if (finiteCheck.IsFailure) return Result<Power>.Failure(finiteCheck);

        if (value < 0)
            return Result<Power>.Validation("Power cannot be negative.");

        return Result<Power>.Success(new Power(value));
    }

    public double Kilowatts => Watts / 1000.0;

    public override string ToString() => $"{Watts:F0} W";
}
