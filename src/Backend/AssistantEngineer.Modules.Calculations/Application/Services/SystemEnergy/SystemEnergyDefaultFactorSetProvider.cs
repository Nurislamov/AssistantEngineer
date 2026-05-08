using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyDefaultFactorSetProvider : ISystemEnergyDefaultFactorSetProvider
{
    private const string Source = "SystemEnergyDefaultFactorSetProvider";

    public SystemEnergyFactorSet GetProjectDefaultFactorSet()
    {
        var baseDiagnostics = new List<StandardCalculationDiagnostic>
        {
            CreateInfo(
                "AE-SYS-DEFAULT-FACTORSET-USED",
                "Project default factor set was used as deterministic internal placeholder data."),
            CreateWarning(
                "AE-SYS-DEFAULT-FACTORS-NOT-COMPLIANCE-DATA",
                "Project default factors are not national-annex/compliance data.")
        };

        IReadOnlyList<StandardCalculationDiagnostic> FactorDiagnostics() =>
        [
            CreateWarning(
                "AE-SYS-DEFAULT-FACTORS-NOT-COMPLIANCE-DATA",
                "This factor is project-default placeholder data and not compliance data.")
        ];

        var factors = new List<SystemEnergyPrimaryEnergyFactor>
        {
            new(
                Carrier: SystemEnergyCarrier.Electricity,
                RenewableFactor: 0.4,
                NonRenewableFactor: 1.8,
                TotalFactor: 2.2,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.NaturalGas,
                RenewableFactor: 0.0,
                NonRenewableFactor: 1.1,
                TotalFactor: 1.1,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.DistrictHeating,
                RenewableFactor: 0.2,
                NonRenewableFactor: 0.9,
                TotalFactor: 1.1,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.DistrictCooling,
                RenewableFactor: 0.1,
                NonRenewableFactor: 1.3,
                TotalFactor: 1.4,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.Biomass,
                RenewableFactor: 0.2,
                NonRenewableFactor: 0.1,
                TotalFactor: 0.3,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.FuelOil,
                RenewableFactor: 0.0,
                NonRenewableFactor: 1.2,
                TotalFactor: 1.2,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.LPG,
                RenewableFactor: 0.0,
                NonRenewableFactor: 1.15,
                TotalFactor: 1.15,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.SolarThermal,
                RenewableFactor: 0.1,
                NonRenewableFactor: 0.0,
                TotalFactor: 0.1,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics()),
            new(
                Carrier: SystemEnergyCarrier.Other,
                RenewableFactor: 0.5,
                NonRenewableFactor: 0.5,
                TotalFactor: 1.0,
                SourceKind: SystemEnergyFactorSourceKind.ProjectDefault,
                Source: "AssistantEngineer.ProjectDefaults",
                Region: null,
                Year: null,
                Diagnostics: FactorDiagnostics())
        };

        return new SystemEnergyFactorSet(
            FactorSetId: "AE-SYS-DEFAULT-FACTORS-V1",
            PrimaryEnergyFactors: factors,
            EmissionFactors: [],
            Region: null,
            Year: null,
            Source: "AssistantEngineer.ProjectDefaults",
            Diagnostics: baseDiagnostics);
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            Source);
}
