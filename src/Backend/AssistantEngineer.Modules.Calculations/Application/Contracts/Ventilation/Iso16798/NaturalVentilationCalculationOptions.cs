namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record NaturalVentilationCalculationOptions(
    NaturalVentilationBranchSelectionMode BranchSelectionMode = NaturalVentilationBranchSelectionMode.SumWindAndStack,
    bool UseDensityCorrection = false,
    bool UseAltitudeDensityCorrection = false,
    double SingleSidedOpeningCoefficient = 1.0,
    double? MaximumAirChangesPerHour = null);
