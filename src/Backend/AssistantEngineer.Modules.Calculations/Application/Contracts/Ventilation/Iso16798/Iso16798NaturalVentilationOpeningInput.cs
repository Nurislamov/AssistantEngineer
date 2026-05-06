namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record Iso16798NaturalVentilationOpeningInput(
    string OpeningId,
    double OpeningAreaM2,
    double OpeningRatio,
    bool IsOpen);
