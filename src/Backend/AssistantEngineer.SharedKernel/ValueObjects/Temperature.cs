using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.SharedKernel.ValueObjects;

public record Temperature
{
    public double Celsius { get; }

    private Temperature(double celsius) => Celsius = celsius;

    public static Result<Temperature> FromCelsius(double value)
    {
        var finiteCheck = Guard.AgainstNonFinite(value, "Temperature");
        if (finiteCheck.IsFailure) return Result<Temperature>.Failure(finiteCheck);

        if (value < -273.15)
            return Result<Temperature>.Validation("Temperature cannot be below absolute zero (-273.15 C).");

        return Result<Temperature>.Success(new Temperature(value));
    }

    public double Kelvin => Celsius + 273.15;
    public double Fahrenheit => Celsius * 9.0 / 5.0 + 32.0;

    public override string ToString() => $"{Celsius:F1} C";
}
