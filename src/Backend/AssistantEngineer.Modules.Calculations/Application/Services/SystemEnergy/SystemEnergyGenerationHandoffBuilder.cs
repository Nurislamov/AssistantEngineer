using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyGenerationHandoffBuilder : ISystemEnergyGenerationHandoffBuilder
{
    private const string Source = "SystemEnergyGenerationHandoffBuilder";

    public SystemEnergyGenerationHandoff Build(SystemEnergyModuleChainResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var diagnostics = new List<StandardCalculationDiagnostic>
        {
            CreateInfo(
                "AE-SYS-GENERATION-HANDOFF-ONLY",
                "System-energy generation handoff is provided for downstream generator/final-energy stages and is not a final/primary energy result.")
        };

        var hourlyByEndUse = result.EndUses.ToDictionary(
            endUseResult => endUseResult.EndUse,
            endUseResult => (IReadOnlyList<double>)endUseResult.HourlySystemLoadBeforeGenerationKWh8760,
            EqualityComparer<SystemEnergyEndUse>.Default);

        var annualByEndUse = result.EndUses.ToDictionary(
            endUseResult => endUseResult.EndUse,
            endUseResult => endUseResult.AnnualSystemLoadBeforeGenerationKWh,
            EqualityComparer<SystemEnergyEndUse>.Default);

        var recoverableByEndUse = result.EndUses.ToDictionary(
            endUseResult => endUseResult.EndUse,
            endUseResult => (IReadOnlyList<double>)endUseResult.HourlyRecoverableLossKWh8760,
            EqualityComparer<SystemEnergyEndUse>.Default);

        var nonRecoverableByEndUse = result.EndUses.ToDictionary(
            endUseResult => endUseResult.EndUse,
            endUseResult => (IReadOnlyList<double>)endUseResult.HourlyNonRecoverableLossKWh8760,
            EqualityComparer<SystemEnergyEndUse>.Default);

        return new SystemEnergyGenerationHandoff(
            CalculationId: result.CalculationId,
            HourlySystemLoadBeforeGenerationByEndUseKWh8760: hourlyByEndUse,
            AnnualSystemLoadBeforeGenerationByEndUseKWh: annualByEndUse,
            HourlyRecoverableLossByEndUseKWh8760: recoverableByEndUse,
            HourlyNonRecoverableLossByEndUseKWh8760: nonRecoverableByEndUse,
            AuxiliaryLoads: result.AuxiliaryLoads,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);
}
