namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso52016ConstructionOptions
{
    public bool UseConstructionLayerMassInput { get; init; } = false;

    public bool UseCalculatedAssemblyUValue { get; init; } = true;

    public bool UseCalculatedAssemblyHeatCapacity { get; init; } = true;

    public double DefaultInternalSurfaceResistanceM2KPerW { get; init; } = 0.13;

    public double DefaultExternalSurfaceResistanceM2KPerW { get; init; } = 0.04;
}
