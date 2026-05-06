using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

public sealed class En15316SystemEnergyChainCalculator
{
    private readonly En15316SystemEnergyReferenceDataProvider _referenceDataProvider;

    public En15316SystemEnergyChainCalculator(
        En15316SystemEnergyReferenceDataProvider referenceDataProvider)
    {
        _referenceDataProvider = referenceDataProvider;
    }

    public Result<En15316SystemEnergyResult> Calculate(En15316SystemEnergyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.EndUses is null || input.EndUses.Count == 0)
            return Result<En15316SystemEnergyResult>.Validation("En15316 system energy input requires at least one end-use item.");

        var diagnostics = new List<En15316SystemEnergyDiagnostics>();
        var assumptions = new List<string>
        {
            "EN15316-inspired modular system energy engineering calculator.",
            "Internal deterministic engineering anchors only.",
            "No full EN 15316 compliance claim.",
            "No external certification claim.",
            "Module chain is useful -> emission -> distribution -> storage -> generation, then auxiliary and recoverable losses.",
            "If both efficiency and loss factor are supplied for a module, efficiency is used."
        };

        ValidateInput(input, diagnostics);
        if (HasErrorDiagnostics(diagnostics))
            return Result<En15316SystemEnergyResult>.Validation(BuildValidationMessage(diagnostics));

        var endUseResults = new List<En15316SystemEnergyEndUseResult>(input.EndUses.Count);
        var finalByEndUse = new Dictionary<En15316EndUse, double>();
        var finalByCarrier = new Dictionary<En15316EnergyCarrier, double>();
        var primaryByCarrier = new Dictionary<En15316EnergyCarrier, double>();
        var renewablePrimaryByCarrier = new Dictionary<En15316EnergyCarrier, double>();
        var nonRenewablePrimaryByCarrier = new Dictionary<En15316EnergyCarrier, double>();

        foreach (var endUse in input.EndUses)
        {
            var endUseResult = CalculateEndUse(endUse, input.DiagnosticsContext, diagnostics);
            endUseResults.Add(endUseResult);

            finalByEndUse[endUse.EndUse] = GetOrDefault(finalByEndUse, endUse.EndUse) + endUseResult.FinalEnergyKWh;
            finalByCarrier[endUse.EnergyCarrier] = GetOrDefault(finalByCarrier, endUse.EnergyCarrier) + endUseResult.FinalEnergyKWh;
            primaryByCarrier[endUse.EnergyCarrier] = GetOrDefault(primaryByCarrier, endUse.EnergyCarrier) + endUseResult.PrimaryEnergyKWh;

            if (endUseResult.RenewablePrimaryEnergyKWh.HasValue)
            {
                renewablePrimaryByCarrier[endUse.EnergyCarrier] =
                    GetOrDefault(renewablePrimaryByCarrier, endUse.EnergyCarrier) + endUseResult.RenewablePrimaryEnergyKWh.Value;
            }

            if (endUseResult.NonRenewablePrimaryEnergyKWh.HasValue)
            {
                nonRenewablePrimaryByCarrier[endUse.EnergyCarrier] =
                    GetOrDefault(nonRenewablePrimaryByCarrier, endUse.EnergyCarrier) + endUseResult.NonRenewablePrimaryEnergyKWh.Value;
            }
        }

        var totalFinal = endUseResults.Sum(item => item.FinalEnergyKWh);
        var totalPrimary = endUseResults.Sum(item => item.PrimaryEnergyKWh);

        return Result<En15316SystemEnergyResult>.Success(
            new En15316SystemEnergyResult(
                EndUses: endUseResults,
                FinalEnergyByEndUseKWh: finalByEndUse.ToDictionary(item => item.Key, item => Round6(item.Value)),
                FinalEnergyByCarrierKWh: finalByCarrier.ToDictionary(item => item.Key, item => Round6(item.Value)),
                PrimaryEnergyByCarrierKWh: primaryByCarrier.ToDictionary(item => item.Key, item => Round6(item.Value)),
                RenewablePrimaryEnergyByCarrierKWh: renewablePrimaryByCarrier.ToDictionary(item => item.Key, item => Round6(item.Value)),
                NonRenewablePrimaryEnergyByCarrierKWh: nonRenewablePrimaryByCarrier.ToDictionary(item => item.Key, item => Round6(item.Value)),
                TotalFinalEnergyKWh: Round6(totalFinal),
                TotalPrimaryEnergyKWh: Round6(totalPrimary),
                Diagnostics: diagnostics,
                AssumptionsUsed: assumptions));
    }

    private En15316SystemEnergyEndUseResult CalculateEndUse(
        En15316SystemEnergyEndUseInput input,
        string? globalContext,
        ICollection<En15316SystemEnergyDiagnostics> diagnostics)
    {
        var context = string.IsNullOrWhiteSpace(input.DiagnosticsContext)
            ? globalContext
            : input.DiagnosticsContext;

        var useful = Math.Max(0, input.UsefulEnergyKWh);

        var emission = CalculateModule(
            downstreamEnergyKWh: useful,
            module: input.Emission,
            moduleCode: "Emission",
            context: context,
            diagnostics: diagnostics);

        var distribution = CalculateModule(
            downstreamEnergyKWh: emission.UpstreamEnergyKWh,
            module: input.Distribution,
            moduleCode: "Distribution",
            context: context,
            diagnostics: diagnostics);

        var storage = CalculateModule(
            downstreamEnergyKWh: distribution.UpstreamEnergyKWh,
            module: input.Storage,
            moduleCode: "Storage",
            context: context,
            diagnostics: diagnostics);

        var defaults = _referenceDataProvider.ResolveGenerationDefaults(input.GenerationTechnology);
        var resolvedGenerationEfficiency = input.GenerationEfficiency ?? defaults.GenerationEfficiency;
        var resolvedGenerationCop = input.GenerationCop ?? defaults.GenerationCop;
        var auxiliary = input.AuxiliaryEnergyKWh > 0
            ? input.AuxiliaryEnergyKWh
            : Math.Max(0, defaults.TypicalAuxiliaryFraction * storage.UpstreamEnergyKWh);

        if (input.AuxiliaryEnergyKWh <= 0 && defaults.TypicalAuxiliaryFraction > 0)
        {
            diagnostics.Add(new En15316SystemEnergyDiagnostics(
                "En15316.Auxiliary.Defaulted",
                $"{FormatContext(context)}Auxiliary energy defaulted from generation technology fraction."));
        }

        var generationInput = storage.UpstreamEnergyKWh;
        double generationFinalEnergy;
        if (resolvedGenerationCop is > 0)
        {
            generationFinalEnergy = generationInput / resolvedGenerationCop.Value;
        }
        else if (resolvedGenerationEfficiency is > 0)
        {
            generationFinalEnergy = generationInput / resolvedGenerationEfficiency.Value;
        }
        else
        {
            generationFinalEnergy = generationInput;
            diagnostics.Add(new En15316SystemEnergyDiagnostics(
                "En15316.Generation.DefaultedToPassThrough",
                $"{FormatContext(context)}Generation COP/efficiency was missing; storage input was carried through as final generation energy."));
        }

        var generationLosses = Math.Max(0, generationFinalEnergy - generationInput);
        var totalLosses = emission.LossesKWh + distribution.LossesKWh + storage.LossesKWh + generationLosses;
        var recoveredLosses = totalLosses * input.RecoveredLossFraction;
        var finalBeforeRecovery = generationFinalEnergy + auxiliary;
        var finalEnergy = Math.Max(0, finalBeforeRecovery - recoveredLosses);

        var primaryFactor = input.PrimaryEnergyFactor ?? 1.0;
        var primaryEnergy = finalEnergy * primaryFactor;
        var renewablePrimary = input.RenewablePrimaryEnergyFactor.HasValue
            ? finalEnergy * input.RenewablePrimaryEnergyFactor.Value
            : (double?)null;
        var nonRenewablePrimary = input.NonRenewablePrimaryEnergyFactor.HasValue
            ? finalEnergy * input.NonRenewablePrimaryEnergyFactor.Value
            : (double?)null;

        return new En15316SystemEnergyEndUseResult(
            EndUse: input.EndUse,
            EnergyCarrier: input.EnergyCarrier,
            GenerationTechnology: input.GenerationTechnology,
            UsefulEnergyKWh: Round6(useful),
            Emission: emission with
            {
                DownstreamEnergyKWh = Round6(emission.DownstreamEnergyKWh),
                UpstreamEnergyKWh = Round6(emission.UpstreamEnergyKWh),
                LossesKWh = Round6(emission.LossesKWh)
            },
            Distribution: distribution with
            {
                DownstreamEnergyKWh = Round6(distribution.DownstreamEnergyKWh),
                UpstreamEnergyKWh = Round6(distribution.UpstreamEnergyKWh),
                LossesKWh = Round6(distribution.LossesKWh)
            },
            Storage: storage with
            {
                DownstreamEnergyKWh = Round6(storage.DownstreamEnergyKWh),
                UpstreamEnergyKWh = Round6(storage.UpstreamEnergyKWh),
                LossesKWh = Round6(storage.LossesKWh)
            },
            GenerationInputEnergyKWh: Round6(generationFinalEnergy),
            GenerationLossesKWh: Round6(generationLosses),
            AuxiliaryEnergyKWh: Round6(auxiliary),
            RecoveredLossesKWh: Round6(recoveredLosses),
            FinalEnergyKWh: Round6(finalEnergy),
            PrimaryEnergyKWh: Round6(primaryEnergy),
            RenewablePrimaryEnergyKWh: renewablePrimary.HasValue ? Round6(renewablePrimary.Value) : null,
            NonRenewablePrimaryEnergyKWh: nonRenewablePrimary.HasValue ? Round6(nonRenewablePrimary.Value) : null);
    }

    private static En15316SystemEnergyModuleResult CalculateModule(
        double downstreamEnergyKWh,
        En15316SystemEnergyModuleInput module,
        string moduleCode,
        string? context,
        ICollection<En15316SystemEnergyDiagnostics> diagnostics)
    {
        if (module.Efficiency is > 0 && module.LossFactor is >= 0)
        {
            diagnostics.Add(new En15316SystemEnergyDiagnostics(
                $"En15316.{moduleCode}.EfficiencyPrecedence",
                $"{FormatContext(context)}Both efficiency and loss factor were supplied for {moduleCode}; efficiency was used."));
        }

        if (module.Efficiency is > 0)
        {
            var upstream = downstreamEnergyKWh / module.Efficiency.Value;
            return new En15316SystemEnergyModuleResult(
                DownstreamEnergyKWh: downstreamEnergyKWh,
                UpstreamEnergyKWh: upstream,
                LossesKWh: Math.Max(0, upstream - downstreamEnergyKWh),
                MethodUsed: "Efficiency");
        }

        if (module.LossFactor is >= 0)
        {
            var upstream = downstreamEnergyKWh * (1.0 + module.LossFactor.Value);
            return new En15316SystemEnergyModuleResult(
                DownstreamEnergyKWh: downstreamEnergyKWh,
                UpstreamEnergyKWh: upstream,
                LossesKWh: Math.Max(0, upstream - downstreamEnergyKWh),
                MethodUsed: "LossFactor");
        }

        diagnostics.Add(new En15316SystemEnergyDiagnostics(
            $"En15316.{moduleCode}.PassThrough",
            $"{FormatContext(context)}{moduleCode} module had no efficiency/loss input; pass-through applied."));

        return new En15316SystemEnergyModuleResult(
            DownstreamEnergyKWh: downstreamEnergyKWh,
            UpstreamEnergyKWh: downstreamEnergyKWh,
            LossesKWh: 0,
            MethodUsed: "PassThrough");
    }

    private static void ValidateInput(
        En15316SystemEnergyInput input,
        ICollection<En15316SystemEnergyDiagnostics> diagnostics)
    {
        foreach (var endUse in input.EndUses)
        {
            var context = string.IsNullOrWhiteSpace(endUse.DiagnosticsContext)
                ? input.DiagnosticsContext
                : endUse.DiagnosticsContext;

            if (endUse.UsefulEnergyKWh < 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidUsefulEnergy",
                    $"{FormatContext(context)}Useful energy cannot be negative for {endUse.EndUse}."));
            }

            if (endUse.AuxiliaryEnergyKWh < 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidAuxiliaryEnergy",
                    $"{FormatContext(context)}Auxiliary energy cannot be negative for {endUse.EndUse}."));
            }

            if (endUse.RecoveredLossFraction is < 0 or > 1)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidRecoveredLossFraction",
                    $"{FormatContext(context)}Recovered loss fraction must be between 0 and 1 for {endUse.EndUse}."));
            }

            ValidateModule(endUse.Emission, "Emission", context, diagnostics);
            ValidateModule(endUse.Distribution, "Distribution", context, diagnostics);
            ValidateModule(endUse.Storage, "Storage", context, diagnostics);

            if (endUse.GenerationEfficiency is <= 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidGenerationEfficiency",
                    $"{FormatContext(context)}Generation efficiency must be greater than zero when supplied for {endUse.EndUse}."));
            }

            if (endUse.GenerationCop is <= 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidGenerationCop",
                    $"{FormatContext(context)}Generation COP must be greater than zero when supplied for {endUse.EndUse}."));
            }

            if (endUse.PrimaryEnergyFactor is <= 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidPrimaryFactor",
                    $"{FormatContext(context)}Primary energy factor must be greater than zero when supplied for {endUse.EndUse}."));
            }

            if (endUse.RenewablePrimaryEnergyFactor is < 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidRenewablePrimaryFactor",
                    $"{FormatContext(context)}Renewable primary energy factor must be non-negative when supplied for {endUse.EndUse}."));
            }

            if (endUse.NonRenewablePrimaryEnergyFactor is < 0)
            {
                diagnostics.Add(new En15316SystemEnergyDiagnostics(
                    "En15316.InvalidNonRenewablePrimaryFactor",
                    $"{FormatContext(context)}Non-renewable primary energy factor must be non-negative when supplied for {endUse.EndUse}."));
            }
        }
    }

    private static void ValidateModule(
        En15316SystemEnergyModuleInput module,
        string moduleName,
        string? context,
        ICollection<En15316SystemEnergyDiagnostics> diagnostics)
    {
        if (module.Efficiency is <= 0)
        {
            diagnostics.Add(new En15316SystemEnergyDiagnostics(
                $"En15316.Invalid{moduleName}Efficiency",
                $"{FormatContext(context)}{moduleName} efficiency must be greater than zero when supplied."));
        }

        if (module.LossFactor < 0)
        {
            diagnostics.Add(new En15316SystemEnergyDiagnostics(
                $"En15316.Invalid{moduleName}LossFactor",
                $"{FormatContext(context)}{moduleName} loss factor must be non-negative when supplied."));
        }
    }

    private static bool HasErrorDiagnostics(IEnumerable<En15316SystemEnergyDiagnostics> diagnostics) =>
        diagnostics.Any(item => item.Code.StartsWith("En15316.Invalid", StringComparison.Ordinal));

    private static string BuildValidationMessage(IEnumerable<En15316SystemEnergyDiagnostics> diagnostics)
    {
        var codes = diagnostics
            .Where(item => item.Code.StartsWith("En15316.Invalid", StringComparison.Ordinal))
            .Select(item => item.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return codes.Length == 0
            ? "EN15316 system energy input validation failed."
            : $"EN15316 system energy input validation failed: {string.Join(", ", codes)}.";
    }

    private static string FormatContext(string? context) =>
        string.IsNullOrWhiteSpace(context) ? string.Empty : $"[{context}] ";

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static double GetOrDefault<TKey>(IReadOnlyDictionary<TKey, double> source, TKey key)
        where TKey : notnull =>
        source.TryGetValue(key, out var value) ? value : 0.0;
}
