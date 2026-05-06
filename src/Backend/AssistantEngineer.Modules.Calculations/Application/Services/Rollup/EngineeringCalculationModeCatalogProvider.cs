using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

public sealed class EngineeringCalculationModeCatalogProvider
{
    private static readonly IReadOnlyList<string> RequiredClaimBoundary =
    [
        "Engineering Core V2 governance and internal release readiness.",
        "Internal deterministic engineering governance only.",
        "Compatibility behavior preserved by default.",
        "Inspired calculation paths remain opt-in.",
        "No full ISO/EN compliance claim.",
        "No pyBuildingEnergy parity claim.",
        "No EnergyPlus parity claim.",
        "No ASHRAE 140 validation claim.",
        "No external certification claim.",
        "No automatic production data mutation."
    ];

    private static readonly IReadOnlyList<string> ForbiddenClaims =
    [
        "full ISO compliance",
        "full EN compliance",
        "full ISO 52016 compliance",
        "full ISO 52010 compliance",
        "full ISO 13370 compliance",
        "full ISO 16798 compliance",
        "full ISO 12831-3 compliance",
        "full EN 15316 compliance",
        "ISO 52016 validated",
        "ISO 52010 validated",
        "ISO 13370 validated",
        "ISO 16798 validated",
        "ISO 12831-3 validated",
        "EN 15316 validated",
        "validated against pyBuildingEnergy",
        "validated against EnergyPlus",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validated",
        "ASHRAE 140 covered",
        "ExternalParityCovered",
        "certified",
        "external certification"
    ];

    public IReadOnlyList<EngineeringCalculationMode> GetCatalog()
    {
        return
        [
            CreateMode(
                modeId: "SOLAR-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.Solar,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["ISO52010-SOLAR-FOUNDATION-INTERNAL-GATE"]),
            CreateMode(
                modeId: "ISO52016-MATRIX-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.Iso52016Matrix,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["ISO52016-MATRIX-INTERNAL-GATE"]),
            CreateMode(
                modeId: "ISO52016-PHYSICAL-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.Iso52016Physical,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["ISO52016-PHYSICAL-INTERNAL-GATE"]),
            CreateMode(
                modeId: "ISO52016-CONSTRUCTION-LAYER-MASS-FOUNDATION",
                domain: EngineeringCalculationModeDomain.Iso52016Physical,
                kind: EngineeringCalculationModeKind.ValidationAnchor,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-ISO52016-CONSTRUCTION-001"]),
            CreateMode(
                modeId: "ISO52016-CONSTRUCTION-LAYER-MASS-OPTIN",
                domain: EngineeringCalculationModeDomain.Iso52016Physical,
                kind: EngineeringCalculationModeKind.InspiredOptIn,
                status: EngineeringCalculationModeStatus.AvailableOptIn,
                isDefault: false,
                isOptIn: true,
                optionFlagName: "Iso52016ConstructionOptions.UseConstructionLayerMassInput",
                stageIds: ["AE-ISO52016-CONSTRUCTION-001", "AE-ISO52016-CONSTRUCTION-002"]),
            CreateMode(
                modeId: "VENTILATION-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.Ventilation,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.Default,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-VENT-002"]),
            CreateMode(
                modeId: "VENTILATION-ISO16798-INSPIRED-OPTIN",
                domain: EngineeringCalculationModeDomain.Ventilation,
                kind: EngineeringCalculationModeKind.InspiredOptIn,
                status: EngineeringCalculationModeStatus.AvailableOptIn,
                isDefault: false,
                isOptIn: true,
                optionFlagName: "NaturalVentilationOptions.UseIso16798InspiredCalculator",
                stageIds: ["AE-VENT-001", "AE-VENT-002"]),
            CreateMode(
                modeId: "GROUND-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.Ground,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.Default,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-GROUND-002"]),
            CreateMode(
                modeId: "GROUND-ISO13370-INSPIRED-OPTIN",
                domain: EngineeringCalculationModeDomain.Ground,
                kind: EngineeringCalculationModeKind.InspiredOptIn,
                status: EngineeringCalculationModeStatus.AvailableOptIn,
                isDefault: false,
                isOptIn: true,
                optionFlagName: "Iso13370GroundHeatTransferOptions.UseIso13370InspiredBoundaryCalculator",
                stageIds: ["AE-GROUND-001", "AE-GROUND-002"]),
            CreateMode(
                modeId: "DHW-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.DomesticHotWater,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.Default,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-DHW-002"]),
            CreateMode(
                modeId: "DHW-ISO12831-INSPIRED-OPTIN",
                domain: EngineeringCalculationModeDomain.DomesticHotWater,
                kind: EngineeringCalculationModeKind.InspiredOptIn,
                status: EngineeringCalculationModeStatus.AvailableOptIn,
                isDefault: false,
                isOptIn: true,
                optionFlagName: "DomesticHotWaterOptions.UseIso12831InspiredCalculator",
                stageIds: ["AE-DHW-001", "AE-DHW-002"]),
            CreateMode(
                modeId: "SYSTEM-ENERGY-COMPATIBILITY-DEFAULT",
                domain: EngineeringCalculationModeDomain.SystemEnergy,
                kind: EngineeringCalculationModeKind.CompatibilityDefault,
                status: EngineeringCalculationModeStatus.Default,
                isDefault: true,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-EN15316-002"]),
            CreateMode(
                modeId: "SYSTEM-ENERGY-EN15316-INSPIRED-OPTIN",
                domain: EngineeringCalculationModeDomain.SystemEnergy,
                kind: EngineeringCalculationModeKind.InspiredOptIn,
                status: EngineeringCalculationModeStatus.AvailableOptIn,
                isDefault: false,
                isOptIn: true,
                optionFlagName: "SystemEnergyOptions.UseEn15316InspiredChain",
                stageIds: ["AE-EN15316-001", "AE-EN15316-002"]),
            CreateMode(
                modeId: "BUILDING-INPUT-VALIDATION-GOVERNANCE",
                domain: EngineeringCalculationModeDomain.BuildingInputValidation,
                kind: EngineeringCalculationModeKind.ValidationAnchor,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-BUI-VALIDATION-001"]),
            CreateMode(
                modeId: "ENGINEERING-GOVERNANCE-STAGE-REGISTRY",
                domain: EngineeringCalculationModeDomain.Governance,
                kind: EngineeringCalculationModeKind.GovernanceAnchor,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-GOVERNANCE-001"]),
            CreateMode(
                modeId: "ENGINEERING-GOVERNANCE-CLAIM-SCANNER",
                domain: EngineeringCalculationModeDomain.Governance,
                kind: EngineeringCalculationModeKind.GovernanceAnchor,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-GOVERNANCE-002"]),
            CreateMode(
                modeId: "ENGINEERING-CORE-V2-RELEASE-READINESS",
                domain: EngineeringCalculationModeDomain.Governance,
                kind: EngineeringCalculationModeKind.ReleaseReadinessGate,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-RELEASE-READINESS-002"]),
            CreateMode(
                modeId: "ENGINEERING-CORPORATE-STATUS-SAMPLE",
                domain: EngineeringCalculationModeDomain.Governance,
                kind: EngineeringCalculationModeKind.StatusDisclosure,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-STATUS-001"]),
            CreateMode(
                modeId: "ISO52016-EXTERNAL-VALIDATION-FRAMEWORK",
                domain: EngineeringCalculationModeDomain.ExternalValidation,
                kind: EngineeringCalculationModeKind.ValidationAnchor,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-VALIDATION-ISO52016-001"]),
            CreateMode(
                modeId: "ISO52016-MANUAL-INDEPENDENT-FIXTURES",
                domain: EngineeringCalculationModeDomain.ExternalValidation,
                kind: EngineeringCalculationModeKind.ValidationAnchor,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-VALIDATION-ISO52016-002"]),
            CreateMode(
                modeId: "ISO52016-PYBE-INSPIRED-METHODOLOGY-INTAKE",
                domain: EngineeringCalculationModeDomain.ExternalValidation,
                kind: EngineeringCalculationModeKind.MethodologyIntake,
                status: EngineeringCalculationModeStatus.ClosedInternalGate,
                isDefault: false,
                isOptIn: false,
                optionFlagName: null,
                stageIds: ["AE-VALIDATION-PYBE-001"])
        ];
    }

    private static EngineeringCalculationMode CreateMode(
        string modeId,
        EngineeringCalculationModeDomain domain,
        EngineeringCalculationModeKind kind,
        EngineeringCalculationModeStatus status,
        bool isDefault,
        bool isOptIn,
        string? optionFlagName,
        IReadOnlyList<string> stageIds)
    {
        var stageStatuses = stageIds
            .Select(stageId => new EngineeringCalculationModeStageStatus(stageId, "closed-internal-gate"))
            .ToArray();

        var disclosure = new EngineeringCalculationModeDisclosure(
            Summary: $"{modeId} is governed as {(isDefault ? "default compatibility" : isOptIn ? "opt-in inspired" : "internal-only")} mode.",
            DefaultOrOptInStatus: isDefault ? "default" : isOptIn ? "opt-in" : "internal-only",
            ClaimBoundary: RequiredClaimBoundary,
            ForbiddenClaims: ForbiddenClaims);

        return new EngineeringCalculationMode(
            ModeId: modeId,
            Domain: domain,
            Kind: kind,
            Status: status,
            IsDefault: isDefault,
            IsOptIn: isOptIn,
            OptionFlagName: optionFlagName,
            Stages: stageStatuses,
            DocumentationFiles:
            [
                "docs/calculations/EnergyCalculationParityVerification.md",
                "docs/calculations/EngineeringCoreV1Scope.md"
            ],
            ManifestFiles:
            [
                "docs/releases/EngineeringCalculationModeComparisonRollupManifest.json"
            ],
            ClaimBoundary: new EngineeringCalculationModeClaimBoundary(RequiredClaimBoundary, ForbiddenClaims),
            Disclosure: disclosure);
    }
}
