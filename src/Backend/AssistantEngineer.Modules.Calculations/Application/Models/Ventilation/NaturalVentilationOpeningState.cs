namespace AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;

public sealed record NaturalVentilationOpeningState(
    bool IsOpen,
    double OpeningFactor,
    double EffectiveOpeningAreaM2,
    string Reason);