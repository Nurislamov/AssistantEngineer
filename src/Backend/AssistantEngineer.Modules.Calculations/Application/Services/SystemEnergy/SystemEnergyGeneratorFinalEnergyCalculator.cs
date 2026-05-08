using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyGeneratorFinalEnergyCalculator : ISystemEnergyGeneratorFinalEnergyCalculator
{
    private const string Source = "SystemEnergyGeneratorFinalEnergyCalculator";

    public SystemEnergyGeneratorResult Calculate(
        SystemEnergyGeneratorInput generator,
        SystemEnergyGeneratorAssignedLoad assignedLoad)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(assignedLoad);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var dispatch = new List<SystemEnergyHourlyGeneratorDispatchResult>();
        var hourlyFinalTotal = SystemEnergyProfileHelper.ZeroProfile();
        var hourlyAuxTotal = SystemEnergyProfileHelper.ZeroProfile();
        var emittedInfoCodes = new HashSet<string>(StringComparer.Ordinal);
        var suppliedByEndUse = new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>();
        var finalByEndUse = new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>();

        var directFinal = SystemEnergyProfileHelper.IsValidProfile(generator.HourlyFinalEnergyProfileKWh8760, SystemEnergyProfileHelper.HoursPerYear)
            ? generator.HourlyFinalEnergyProfileKWh8760!.ToArray()
            : SystemEnergyProfileHelper.ZeroProfile();

        var unmetDetected = false;
        var allHandoffOnly = true;
        var allDisabled = true;

        foreach (var endUseEntry in assignedLoad.HourlyAssignedLoadByEndUseKWh8760)
        {
            var endUse = endUseEntry.Key;
            var assigned = SystemEnergyProfileHelper.Ensure8760(endUseEntry.Value);
            var supplied = SystemEnergyProfileHelper.ZeroProfile();
            var final = SystemEnergyProfileHelper.ZeroProfile();
            var auxiliary = SystemEnergyProfileHelper.ZeroProfile();

            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                var requested = Math.Max(0.0, assigned[hour]);
                var cap = generator.NominalCapacityKWhPerHour is > 0.0 ? generator.NominalCapacityKWhPerHour.Value : double.PositiveInfinity;
                var cappedSupplied = requested;
                if (double.IsFinite(cap) && requested > cap)
                {
                    cappedSupplied = cap;
                    unmetDetected = true;
                    diagnostics.Add(CreateWarning(
                        "AE-SYS-GEN-CAPACITY-CAPPED",
                        $"Generator '{generator.GeneratorId}' capped by nominal capacity at hour {hour} for end use '{endUse}'."));
                }

                var status = SystemEnergyFinalEnergyStatus.Calculated;
                var unmet = Math.Max(0.0, requested - cappedSupplied);
                var finalEnergy = 0.0;

                switch (generator.CalculationMode)
                {
                    case SystemEnergyGeneratorCalculationMode.Disabled:
                        status = SystemEnergyFinalEnergyStatus.Disabled;
                        cappedSupplied = 0.0;
                        unmet = requested;
                        unmetDetected = unmet > 0.0 || unmetDetected;
                        diagnostics.Add(CreateInfo("AE-SYS-GEN-DISABLED", $"Generator '{generator.GeneratorId}' is disabled."));
                        break;

                    case SystemEnergyGeneratorCalculationMode.FixedEfficiency:
                        var efficiency = generator.Efficiency;
                        if (generator.GeneratorKind == SystemEnergyGeneratorKind.ElectricResistance && (!efficiency.HasValue || efficiency.Value <= 0.0))
                        {
                            efficiency = 1.0;
                            diagnostics.Add(CreateInfo(
                                "AE-SYS-GEN-ELECTRIC-RESISTANCE-EFFICIENCY-DEFAULTED",
                                $"Generator '{generator.GeneratorId}' electric-resistance efficiency defaulted to 1.0."));
                        }

                        if (efficiency is > 0.0 and <= 1.0)
                        {
                            finalEnergy = cappedSupplied / efficiency.Value;
                            EmitInfoOnce("AE-SYS-GEN-FIXED-EFFICIENCY-USED", $"Generator '{generator.GeneratorId}' applied fixed efficiency.");
                        }
                        else
                        {
                            cappedSupplied = 0.0;
                            unmet = requested;
                            status = SystemEnergyFinalEnergyStatus.NotCalculable;
                            unmetDetected = unmet > 0.0 || unmetDetected;
                            diagnostics.Add(CreateWarning("AE-SYS-GEN-EFFICIENCY-INVALID", $"Generator '{generator.GeneratorId}' has invalid efficiency for fixed-efficiency mode."));
                        }

                        break;

                    case SystemEnergyGeneratorCalculationMode.FixedCop:
                        if (generator.Cop is > 0.0)
                        {
                            finalEnergy = cappedSupplied / generator.Cop.Value;
                            EmitInfoOnce("AE-SYS-GEN-FIXED-COP-USED", $"Generator '{generator.GeneratorId}' applied fixed COP.");
                        }
                        else
                        {
                            cappedSupplied = 0.0;
                            unmet = requested;
                            status = SystemEnergyFinalEnergyStatus.NotCalculable;
                            unmetDetected = unmet > 0.0 || unmetDetected;
                            diagnostics.Add(CreateWarning("AE-SYS-GEN-COP-INVALID", $"Generator '{generator.GeneratorId}' has invalid COP."));
                        }

                        break;

                    case SystemEnergyGeneratorCalculationMode.FixedEer:
                        if (generator.Eer is > 0.0)
                        {
                            finalEnergy = cappedSupplied / generator.Eer.Value;
                            EmitInfoOnce("AE-SYS-GEN-FIXED-EER-USED", $"Generator '{generator.GeneratorId}' applied fixed EER.");
                        }
                        else
                        {
                            cappedSupplied = 0.0;
                            unmet = requested;
                            status = SystemEnergyFinalEnergyStatus.NotCalculable;
                            unmetDetected = unmet > 0.0 || unmetDetected;
                            diagnostics.Add(CreateWarning("AE-SYS-GEN-EER-INVALID", $"Generator '{generator.GeneratorId}' has invalid EER."));
                        }

                        break;

                    case SystemEnergyGeneratorCalculationMode.SeasonalPerformanceFactor:
                        if (generator.SeasonalPerformanceFactor is > 0.0)
                        {
                            finalEnergy = cappedSupplied / generator.SeasonalPerformanceFactor.Value;
                            EmitInfoOnce("AE-SYS-GEN-SPF-USED", $"Generator '{generator.GeneratorId}' applied seasonal performance factor.");
                        }
                        else
                        {
                            cappedSupplied = 0.0;
                            unmet = requested;
                            status = SystemEnergyFinalEnergyStatus.NotCalculable;
                            unmetDetected = unmet > 0.0 || unmetDetected;
                            diagnostics.Add(CreateWarning("AE-SYS-GEN-SPF-INVALID", $"Generator '{generator.GeneratorId}' has invalid seasonal performance factor."));
                        }

                        break;

                    case SystemEnergyGeneratorCalculationMode.DirectFinalEnergyProfile:
                        finalEnergy = directFinal[hour];
                        EmitInfoOnce("AE-SYS-GEN-DIRECT-FINAL-PROFILE-USED", $"Generator '{generator.GeneratorId}' used direct final-energy profile.");
                        break;

                    case SystemEnergyGeneratorCalculationMode.DistrictHandoff:
                        finalEnergy = cappedSupplied;
                        EmitInfoOnce("AE-SYS-GEN-DISTRICT-HANDOFF-USED", $"Generator '{generator.GeneratorId}' used district handoff behavior.");
                        break;

                    case SystemEnergyGeneratorCalculationMode.CustomFactor:
                        if (generator.Efficiency is > 0.0 and <= 1.0)
                        {
                            finalEnergy = cappedSupplied / generator.Efficiency.Value;
                        }
                        else if (generator.Cop is > 0.0)
                        {
                            finalEnergy = cappedSupplied / generator.Cop.Value;
                        }
                        else if (generator.SeasonalPerformanceFactor is > 0.0)
                        {
                            finalEnergy = cappedSupplied / generator.SeasonalPerformanceFactor.Value;
                        }
                        else
                        {
                            cappedSupplied = 0.0;
                            unmet = requested;
                            status = SystemEnergyFinalEnergyStatus.NotCalculable;
                            unmetDetected = unmet > 0.0 || unmetDetected;
                        }

                        EmitInfoOnce("AE-SYS-GEN-CUSTOM-FACTOR-USED", $"Generator '{generator.GeneratorId}' used custom factor mode.");
                        break;

                    case SystemEnergyGeneratorCalculationMode.HandoffOnly:
                        status = SystemEnergyFinalEnergyStatus.HandoffOnly;
                        finalEnergy = 0.0;
                        EmitInfoOnce("AE-SYS-GEN-HANDOFF-ONLY", $"Generator '{generator.GeneratorId}' is handoff-only.");
                        break;

                    case SystemEnergyGeneratorCalculationMode.Other:
                    case SystemEnergyGeneratorCalculationMode.Unknown:
                    default:
                        status = SystemEnergyFinalEnergyStatus.NotCalculable;
                        cappedSupplied = 0.0;
                        unmet = requested;
                        unmetDetected = unmet > 0.0 || unmetDetected;
                        diagnostics.Add(CreateWarning("AE-SYS-GEN-UNKNOWN-NO-FALLBACK", $"Generator '{generator.GeneratorId}' mode is unsupported and did not apply fallback."));
                        break;
                }

                var auxiliaryValue = CalculateAuxiliary(generator, cappedSupplied, diagnostics);
                auxiliary[hour] = auxiliaryValue;
                supplied[hour] = cappedSupplied;
                final[hour] = finalEnergy;
                hourlyFinalTotal[hour] += finalEnergy;
                hourlyAuxTotal[hour] += auxiliaryValue;

                dispatch.Add(new SystemEnergyHourlyGeneratorDispatchResult(
                    HourIndex: hour,
                    GeneratorId: generator.GeneratorId,
                    GeneratorKind: generator.GeneratorKind,
                    EndUse: endUse,
                    RequestedSystemLoadKWh: requested,
                    SuppliedSystemLoadKWh: cappedSupplied,
                    UnmetSystemLoadKWh: Math.Max(0.0, unmet),
                    FinalEnergyKWh: finalEnergy,
                    FinalEnergyCarrier: generator.FinalEnergyCarrier,
                    AuxiliaryElectricityKWh: auxiliaryValue,
                    Status: status,
                    Diagnostics: []));

                allHandoffOnly &= status == SystemEnergyFinalEnergyStatus.HandoffOnly;
                allDisabled &= status == SystemEnergyFinalEnergyStatus.Disabled;
            }

            suppliedByEndUse[endUse] = supplied;
            finalByEndUse[endUse] = final;
        }

        var monthlyFinal = SystemEnergyProfileHelper.AggregateMonthly(hourlyFinalTotal);
        var monthlyAux = SystemEnergyProfileHelper.AggregateMonthly(hourlyAuxTotal);

        var overallStatus = SystemEnergyFinalEnergyStatus.Calculated;
        if (allDisabled)
            overallStatus = SystemEnergyFinalEnergyStatus.Disabled;
        else if (allHandoffOnly)
            overallStatus = SystemEnergyFinalEnergyStatus.HandoffOnly;
        else if (unmetDetected)
            overallStatus = SystemEnergyFinalEnergyStatus.PartiallyCalculated;
        else if (hourlyFinalTotal.All(value => value <= 0.0))
            overallStatus = SystemEnergyFinalEnergyStatus.NotCalculable;

        diagnostics.Add(CreateInfo(
            "AE-SYS-GEN-FINAL-ENERGY-CALCULATED",
            $"Generator '{generator.GeneratorId}' final-energy result was calculated."));

        return new SystemEnergyGeneratorResult(
            GeneratorId: generator.GeneratorId,
            Name: generator.Name,
            GeneratorKind: generator.GeneratorKind,
            CalculationMode: generator.CalculationMode,
            FinalEnergyCarrier: generator.FinalEnergyCarrier,
            ServedEndUses: generator.ServedEndUses,
            HourlyDispatch: dispatch,
            HourlySuppliedSystemLoadByEndUseKWh8760: suppliedByEndUse,
            HourlyFinalEnergyByEndUseKWh8760: finalByEndUse,
            HourlyTotalFinalEnergyKWh8760: hourlyFinalTotal,
            HourlyTotalAuxiliaryElectricityKWh8760: hourlyAuxTotal,
            AnnualSuppliedSystemLoadKWh: suppliedByEndUse.Values.Sum(profile => profile.Sum()),
            AnnualFinalEnergyKWh: hourlyFinalTotal.Sum(),
            AnnualAuxiliaryElectricityKWh: hourlyAuxTotal.Sum(),
            MonthlyFinalEnergyKWh: monthlyFinal,
            MonthlyAuxiliaryElectricityKWh: monthlyAux,
            Status: overallStatus,
            Diagnostics: diagnostics);

        void EmitInfoOnce(string code, string message)
        {
            if (emittedInfoCodes.Add(code))
                diagnostics.Add(CreateInfo(code, message));
        }
    }

    private static double CalculateAuxiliary(
        SystemEnergyGeneratorInput generator,
        double suppliedSystemLoadKWh,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (generator.AuxiliaryElectricityKWhPerKWhOutput is >= 0.0)
        {
            if (!diagnostics.Any(diagnostic => diagnostic.Code == "AE-SYS-GEN-AUXILIARY-KWH-PER-KWH-USED"))
            {
                diagnostics.Add(CreateInfo(
                    "AE-SYS-GEN-AUXILIARY-KWH-PER-KWH-USED",
                    $"Generator '{generator.GeneratorId}' auxiliary kWh-per-kWh-output factor was used."));
            }
            return suppliedSystemLoadKWh * generator.AuxiliaryElectricityKWhPerKWhOutput.Value;
        }

        if (generator.AuxiliaryElectricityFraction is >= 0.0)
        {
            if (!diagnostics.Any(diagnostic => diagnostic.Code == "AE-SYS-GEN-AUXILIARY-FRACTION-USED"))
            {
                diagnostics.Add(CreateInfo(
                    "AE-SYS-GEN-AUXILIARY-FRACTION-USED",
                    $"Generator '{generator.GeneratorId}' auxiliary fraction was used."));
            }
            return suppliedSystemLoadKWh * generator.AuxiliaryElectricityFraction.Value;
        }

        return 0.0;
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
