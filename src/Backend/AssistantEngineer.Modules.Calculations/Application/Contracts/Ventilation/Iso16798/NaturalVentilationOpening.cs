namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record NaturalVentilationOpening(
    string OpeningId,
    double OpeningAreaM2,
    double OpeningFraction,
    bool IsOpen = true,
    double? OpeningHeightM = null,
    double? DischargeCoefficient = null);
