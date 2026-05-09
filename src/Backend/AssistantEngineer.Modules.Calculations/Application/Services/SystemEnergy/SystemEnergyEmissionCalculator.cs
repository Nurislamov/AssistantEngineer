using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyEmissionCalculator : ISystemEnergyEmissionCalculator
{
    private const string Source = "SystemEnergyEmissionCalculator";

    public SystemEnergyStageCalculationResult Calculate(SystemEnergyStageCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stage = request.StageDefinition with
        {
            SubsystemKind = SystemEnergySubsystemKind.Emission
        };

        if (request.StageDefinition.SubsystemKind != SystemEnergySubsystemKind.Emission)
        {
            var diagnostics = new List<StandardCalculationDiagnostic>
            {
                CreateWarning(
                    "AE-SYS-EMISSION-STAGE-KIND-CORRECTED",
                    $"Stage '{request.StageDefinition.StageId}' was evaluated as Emission subsystem.")
            };

            var result = SystemEnergyStageCalculatorCore.CalculateStage(request with
            {
                StageDefinition = stage
            }, Source);

            return result with
            {
                Diagnostics = SystemEnergyStageCalculatorCore.SortDiagnostics(result.Diagnostics.Concat(diagnostics))
            };
        }

        return SystemEnergyStageCalculatorCore.CalculateStage(request, Source);
    }

    public IReadOnlyList<SystemEnergyEmissionResult> Calculate(
        SystemEnergyFinalEnergyResult finalEnergyResult,
        SystemEnergyFactorSet factorSet)
    {
        ArgumentNullException.ThrowIfNull(finalEnergyResult);
        ArgumentNullException.ThrowIfNull(factorSet);

        if (factorSet.EmissionFactors.Count == 0)
            return [];

        var results = new List<SystemEnergyEmissionResult>();

        foreach (var carrierEntry in finalEnergyResult.HourlyFinalEnergyByCarrierKWh8760)
        {
            var carrier = carrierEntry.Key;
            var hourlyFinal = SystemEnergyProfileHelper.Ensure8760(carrierEntry.Value);
            var matchingFactors = factorSet.EmissionFactors
                .Where(factor => factor.Carrier == carrier)
                .ToArray();

            if (matchingFactors.Length == 0)
            {
                var missingDiagnostics = new List<StandardCalculationDiagnostic>
                {
                    CreateWarning(
                        "AE-SYS-EMISSION-FACTOR-MISSING",
                        $"No emission factor was provided for carrier '{carrier}'."),
                    CreateWarning(
                        "AE-SYS-EMISSION-NOT-COMPLIANCE-DATA",
                        "Emission results are disclosure/reporting values and not compliance data without external validation.")
                };

                results.Add(new SystemEnergyEmissionResult(
                    Carrier: carrier,
                    FactorKind: SystemEnergyEmissionFactorKind.Unknown,
                    HourlyEmissionsKg8760: SystemEnergyProfileHelper.ZeroProfile(),
                    MonthlyEmissionsKg: SystemEnergyProfileHelper.AggregateMonthly(SystemEnergyProfileHelper.ZeroProfile()),
                    AnnualEmissionsKg: 0.0,
                    Factor: new SystemEnergyEmissionFactor(
                        Carrier: carrier,
                        FactorKind: SystemEnergyEmissionFactorKind.Unknown,
                        KgPerKWh: 0.0,
                        SourceKind: SystemEnergyFactorSourceKind.Unknown,
                        Source: "MissingEmissionFactor",
                        Region: factorSet.Region,
                        Year: factorSet.Year,
                        Diagnostics: []),
                    Diagnostics: missingDiagnostics));
                continue;
            }

            foreach (var factor in matchingFactors)
            {
                var diagnostics = new List<StandardCalculationDiagnostic>();
                diagnostics.AddRange(factor.Diagnostics);
                diagnostics.Add(CreateInfo(
                    "AE-SYS-EMISSION-CARRIER-CALCULATED",
                    $"Emissions were calculated for carrier '{carrier}' and factor kind '{factor.FactorKind}'."));
                diagnostics.Add(CreateWarning(
                    "AE-SYS-EMISSION-NOT-COMPLIANCE-DATA",
                    "Emission results are disclosure/reporting values and not compliance data without external validation."));

                var hourlyEmissions = new double[SystemEnergyProfileHelper.HoursPerYear];
                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    hourlyEmissions[hour] = hourlyFinal[hour] * factor.KgPerKWh;
                }

                var monthlyEmissions = SystemEnergyProfileHelper.AggregateMonthly(hourlyEmissions);
                results.Add(new SystemEnergyEmissionResult(
                    Carrier: carrier,
                    FactorKind: factor.FactorKind,
                    HourlyEmissionsKg8760: hourlyEmissions,
                    MonthlyEmissionsKg: monthlyEmissions,
                    AnnualEmissionsKg: hourlyEmissions.Sum(),
                    Factor: factor,
                    Diagnostics: diagnostics));
            }
        }

        return results;
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
