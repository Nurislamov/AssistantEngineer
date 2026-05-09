namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDrawOffProfileRequest(
    DomesticHotWaterDemandDefinition DemandDefinition,
    DomesticHotWaterDrawOffProfileResolution Resolution,
    int NumberOfSteps,
    IReadOnlyList<double>? Schedule,
    DomesticHotWaterScheduleNormalizationMode NormalizationMode,
    DomesticHotWaterFallbackProfileMode FallbackProfileMode,
    DomesticHotWaterDiagnosticsMode DiagnosticsMode);
