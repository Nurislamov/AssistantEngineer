namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016InternalGainCalculationResult(
    string UseType,
    double FloorAreaM2,
    double OccupantGainW,
    double LightingGainW,
    double EquipmentGainW,
    double TotalSensibleGainW,
    double ConvectiveGainW,
    double RadiativeGainW);