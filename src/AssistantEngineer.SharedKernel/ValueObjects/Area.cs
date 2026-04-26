using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.SharedKernel.ValueObjects;

public record Area
{
    public double SquareMeters { get; }

    private Area(double squareMeters) => SquareMeters = squareMeters;

    public static Result<Area> FromSquareMeters(double value)
    {
        var finiteCheck = Guard.AgainstNonFinite(value, "Area");
        if (finiteCheck.IsFailure) return Result<Area>.Failure(finiteCheck);

        if (value <= 0)
            return Result<Area>.Validation("Area must be greater than zero.");

        return Result<Area>.Success(new Area(value));
    }

    public override string ToString() => $"{SquareMeters:F2} m2";
}
