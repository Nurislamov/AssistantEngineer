namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record SlabOnGroundGeometry(
    double FloorAreaM2,
    double ExposedPerimeterM,
    double SlabThermalResistanceM2KPerW);

