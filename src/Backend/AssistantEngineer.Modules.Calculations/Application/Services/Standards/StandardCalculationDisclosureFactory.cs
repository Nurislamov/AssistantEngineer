using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Standards;

public sealed class StandardCalculationDisclosureFactory : IStandardCalculationDisclosureFactory
{
    private static readonly IReadOnlyList<string> DefaultForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private static readonly IReadOnlyList<string> DefaultAllowedClaims =
    [
        "Internal deterministic engineering assumptions.",
        "Standard-inspired methodological lane only.",
        "Calculation-contract and diagnostics foundation stage."
    ];

    private static readonly IReadOnlyList<string> DefaultLimitations =
    [
        "Foundation stage does not implement full standard formulas.",
        "Outputs require explicit assumption/source disclosure."
    ];

    private static readonly IReadOnlyList<string> DefaultAssumptions =
    [
        "Reference data placeholders may be used where normative tables are not implemented.",
        "Fallback diagnostics are emitted when applicable."
    ];

    public StandardCalculationDisclosure CreateThermalZonesDisclosure() =>
        CreateDisclosure(
            family: StandardCalculationFamily.InternalEngineering,
            stage: StandardCalculationStage.Foundation,
            mode: StandardCalculationMode.InternalEngineering,
            calculationPath: "ThermalZones/AdjacentBoundaries/Foundation");

    public StandardCalculationDisclosure CreateNaturalVentilationEn16798Disclosure() =>
        CreateDisclosure(
            family: StandardCalculationFamily.EN16798,
            stage: StandardCalculationStage.Ventilation,
            mode: StandardCalculationMode.StandardInspired,
            calculationPath: "Ventilation/EN16798/InspiredLane");

    public StandardCalculationDisclosure CreateGroundIso13370Disclosure() =>
        CreateDisclosure(
            family: StandardCalculationFamily.ISO13370,
            stage: StandardCalculationStage.BoundaryCondition,
            mode: StandardCalculationMode.StandardInspired,
            calculationPath: "Ground/ISO13370/InspiredLane");

    public StandardCalculationDisclosure CreateDomesticHotWaterIso12831Disclosure() =>
        CreateDisclosure(
            family: StandardCalculationFamily.ISO12831,
            stage: StandardCalculationStage.DomesticHotWater,
            mode: StandardCalculationMode.StandardInspired,
            calculationPath: "DomesticHotWater/ISO12831-3/InspiredLane");

    public StandardCalculationDisclosure CreateSystemEnergyEn15316Disclosure() =>
        CreateDisclosure(
            family: StandardCalculationFamily.EN15316,
            stage: StandardCalculationStage.SystemEnergy,
            mode: StandardCalculationMode.StandardInspired,
            calculationPath: "SystemEnergy/EN15316/InspiredLane");

    private static StandardCalculationDisclosure CreateDisclosure(
        StandardCalculationFamily family,
        StandardCalculationStage stage,
        StandardCalculationMode mode,
        string calculationPath)
    {
        var diagnostics = new[]
        {
            new StandardCalculationDiagnostic(
                Severity: CalculationDiagnosticSeverity.Info,
                Code: "StandardsFoundation.Disclosure.Default",
                Message: "Default standard disclosure was generated for foundation-stage governance.",
                Context: calculationPath,
                Source: "StandardCalculationDisclosureFactory",
                Family: family,
                Stage: stage)
        };

        return new StandardCalculationDisclosure(
            Family: family,
            Stage: stage,
            Mode: mode,
            CalculationPath: calculationPath,
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims: DefaultAllowedClaims,
                ForbiddenClaims: DefaultForbiddenClaims,
                Limitations: DefaultLimitations,
                Assumptions: DefaultAssumptions),
            Diagnostics: diagnostics);
    }
}
