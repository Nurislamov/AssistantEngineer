using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyFactorSetValidator : ISystemEnergyFactorSetValidator
{
    private const double TotalMismatchTolerance = 1e-6;
    private const string Source = "SystemEnergyFactorSetValidator";

    public SystemEnergyFactorSetValidationResult Validate(SystemEnergyFactorSet factorSet)
    {
        ArgumentNullException.ThrowIfNull(factorSet);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(factorSet.Diagnostics);

        if (string.IsNullOrWhiteSpace(factorSet.FactorSetId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-FACTORSET-ID-MISSING",
                "Factor-set id is required."));
        }

        if (factorSet.PrimaryEnergyFactors.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-PRIMARY-FACTORS-MISSING",
                "At least one primary-energy factor is required."));
        }

        var seenPrimaryCarriers = new HashSet<SystemEnergyCarrier>();
        foreach (var factor in factorSet.PrimaryEnergyFactors)
        {
            diagnostics.AddRange(factor.Diagnostics);

            if (factor.Carrier == SystemEnergyCarrier.Unknown)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-FACTOR-CARRIER-UNKNOWN",
                    "Primary-energy factor carrier cannot be Unknown."));
            }

            if (!seenPrimaryCarriers.Add(factor.Carrier))
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-FACTOR-DUPLICATE-CARRIER",
                    $"Duplicate primary-energy factor found for carrier '{factor.Carrier}'."));
            }

            ValidateNonNegative(factor.RenewableFactor, "RenewableFactor", diagnostics);
            ValidateNonNegative(factor.NonRenewableFactor, "NonRenewableFactor", diagnostics);
            ValidateNonNegative(factor.TotalFactor, "TotalFactor", diagnostics);

            if (double.IsFinite(factor.RenewableFactor) &&
                double.IsFinite(factor.NonRenewableFactor) &&
                double.IsFinite(factor.TotalFactor))
            {
                var expectedTotal = factor.RenewableFactor + factor.NonRenewableFactor;
                if (Math.Abs(expectedTotal - factor.TotalFactor) > TotalMismatchTolerance)
                {
                    diagnostics.Add(CreateWarning(
                        "AE-SYS-FACTOR-TOTAL-MISMATCH",
                        $"Primary-energy factor total mismatch for carrier '{factor.Carrier}'. Supplied total '{factor.TotalFactor}' differs from renewable + non-renewable '{expectedTotal}'."));
                }
            }

            ValidateSourceKind(factor.SourceKind, diagnostics);
        }

        var seenEmissionFactors = new HashSet<(SystemEnergyCarrier Carrier, SystemEnergyEmissionFactorKind FactorKind)>();
        foreach (var factor in factorSet.EmissionFactors)
        {
            diagnostics.AddRange(factor.Diagnostics);

            if (factor.Carrier == SystemEnergyCarrier.Unknown)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-EMISSION-FACTOR-CARRIER-UNKNOWN",
                    "Emission factor carrier cannot be Unknown."));
            }

            if (!double.IsFinite(factor.KgPerKWh) || factor.KgPerKWh < 0.0)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-EMISSION-FACTOR-NEGATIVE",
                    $"Emission factor for carrier '{factor.Carrier}' must be finite and >= 0."));
            }

            if (!seenEmissionFactors.Add((factor.Carrier, factor.FactorKind)))
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-EMISSION-FACTOR-DUPLICATE",
                    $"Duplicate emission factor found for carrier '{factor.Carrier}' and kind '{factor.FactorKind}'."));
            }

            ValidateSourceKind(factor.SourceKind, diagnostics);
        }

        return new SystemEnergyFactorSetValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateSourceKind(
        SystemEnergyFactorSourceKind sourceKind,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (sourceKind == SystemEnergyFactorSourceKind.Unknown)
        {
            diagnostics.Add(CreateWarning(
                "AE-SYS-FACTOR-SOURCE-UNKNOWN",
                "Factor source kind is Unknown for a calculation-ready factor-set input."));
        }

        if (sourceKind == SystemEnergyFactorSourceKind.NationalAnnexPlaceholder)
        {
            diagnostics.Add(CreateWarning(
                "AE-SYS-FACTOR-NATIONAL-ANNEX-PLACEHOLDER-NOT-COMPLIANCE",
                "National-annex placeholder factor data is not compliance data and requires external validation."));
        }
    }

    private static void ValidateNonNegative(
        double value,
        string fieldName,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-FACTOR-NEGATIVE",
                $"Primary-energy factor field '{fieldName}' must be finite and >= 0."));
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
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
