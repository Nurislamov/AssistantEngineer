using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyStorageCalculator : ISystemEnergyStorageCalculator
{
    private const string Source = "SystemEnergyStorageCalculator";

    public SystemEnergyStageCalculationResult Calculate(SystemEnergyStageCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UseKind == SystemEnergyUseKind.DomesticHotWater &&
            request.OwnershipPolicy is SystemEnergyLossOwnershipPolicy.UpstreamOwnsLosses or SystemEnergyLossOwnershipPolicy.NoDoubleCounting)
        {
            var input = request.InputProfileKWh
                .Select(value => double.IsFinite(value) && value > 0.0 ? value : 0.0)
                .ToArray();

            return new SystemEnergyStageCalculationResult(
                SubsystemKind: SystemEnergySubsystemKind.Storage,
                UseKind: request.UseKind,
                InputProfileKWh: input,
                OutputProfileKWh: input,
                LossesProfileKWh: new double[input.Length],
                RecoveredLossesProfileKWh: new double[input.Length],
                AuxiliaryEnergyProfileKWh: new double[input.Length],
                Diagnostics:
                [
                    SystemEnergyDiagnosticsFactory.Create(
                        CalculationDiagnosticSeverity.Info,
                        "AE-SYS-STORAGE-SKIPPED-UPSTREAM-OWNERSHIP",
                        "Storage losses were skipped because DHW upstream ownership/no-double-counting policy is active.",
                        Source)
                ],
                Assumptions:
                [
                    "DHW upstream ownership policy prevents second storage-loss application in system-energy chain."
                ],
                Warnings: []);
        }

        var result = SystemEnergyStageCalculatorCore.CalculateStage(request with
        {
            StageDefinition = request.StageDefinition with { SubsystemKind = SystemEnergySubsystemKind.Storage }
        }, Source);

        var diagnostics = result.Diagnostics.ToList();
        if (request.StageDefinition.SubsystemKind != SystemEnergySubsystemKind.Storage)
        {
            diagnostics.Add(SystemEnergyDiagnosticsFactory.Create(
                CalculationDiagnosticSeverity.Warning,
                "AE-SYS-STORAGE-STAGE-KIND-CORRECTED",
                $"Stage '{request.StageDefinition.StageId}' was evaluated as Storage subsystem.",
                Source));
        }

        return result with
        {
            Diagnostics = SystemEnergyStageCalculatorCore.SortDiagnostics(diagnostics)
        };
    }
}
