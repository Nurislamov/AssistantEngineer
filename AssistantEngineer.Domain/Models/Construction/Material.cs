using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Domain.Models.Construction;

public class Material
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public double ThermalConductivityWPerMK { get; private set; }
    public double DensityKgPerM3 { get; private set; }
    public double SpecificHeatJPerKgK { get; private set; }

    private Material() { }

    private Material(
        string name,
        double thermalConductivityWPerMK,
        double densityKgPerM3,
        double specificHeatJPerKgK)
    {
        Name = name;
        ThermalConductivityWPerMK = thermalConductivityWPerMK;
        DensityKgPerM3 = densityKgPerM3;
        SpecificHeatJPerKgK = specificHeatJPerKgK;
    }

    public double VolumetricHeatCapacityKjPerM3K => DensityKgPerM3 * SpecificHeatJPerKgK / 1000.0;

    public static Result<Material> Create(
        string name,
        double thermalConductivityWPerMK,
        double densityKgPerM3,
        double specificHeatJPerKgK)
    {
        var nameResult = name.ToRequiredTrimmed("Material name", maxLength: 200);
        if (nameResult.IsFailure) return Result<Material>.Failure(nameResult);

        var conductivityCheck = Guard.AgainstZeroOrNegative(thermalConductivityWPerMK, "Thermal conductivity");
        if (conductivityCheck.IsFailure) return Result<Material>.Failure(conductivityCheck);

        var densityCheck = Guard.AgainstZeroOrNegative(densityKgPerM3, "Density");
        if (densityCheck.IsFailure) return Result<Material>.Failure(densityCheck);

        var specificHeatCheck = Guard.AgainstZeroOrNegative(specificHeatJPerKgK, "Specific heat");
        if (specificHeatCheck.IsFailure) return Result<Material>.Failure(specificHeatCheck);

        return Result<Material>.Success(new Material(
            nameResult.Value,
            thermalConductivityWPerMK,
            densityKgPerM3,
            specificHeatJPerKgK));
    }
}
