using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;

public sealed class AnnualProfileShapeValidator : IAnnualProfileShapeValidator
{
    public AnnualProfileShapeValidationResult ValidateHourlyNonLeapProfile(
        IReadOnlyList<double> values,
        bool requireNonNegative = false,
        string? source = null) =>
        Validate(values, expectedCount: 8760, requireNonNegative, source ?? "HourlyNonLeapProfile");

    public AnnualProfileShapeValidationResult ValidateMonthlyProfile(
        IReadOnlyList<double> values,
        bool requireNonNegative = false,
        string? source = null) =>
        Validate(values, expectedCount: 12, requireNonNegative, source ?? "MonthlyProfile");

    public AnnualProfileShapeValidationResult ValidateDailyProfile(
        IReadOnlyList<double> values,
        bool requireNonNegative = false,
        string? source = null) =>
        Validate(values, expectedCount: 24, requireNonNegative, source ?? "DailyProfile");

    private static AnnualProfileShapeValidationResult Validate(
        IReadOnlyList<double> values,
        int expectedCount,
        bool requireNonNegative,
        string source)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (values.Count != expectedCount)
        {
            diagnostics.Add(CreateDiagnostic(
                CalculationDiagnosticSeverity.Error,
                "Standards.ProfileShape.CountMismatch",
                $"Profile expected {expectedCount} values but received {values.Count}.",
                source));
        }

        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (!double.IsFinite(value))
            {
                diagnostics.Add(CreateDiagnostic(
                    CalculationDiagnosticSeverity.Error,
                    "Standards.ProfileShape.NonFiniteValue",
                    $"Profile contains non-finite value at index {index}.",
                    source));
            }

            if (requireNonNegative && value < 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    CalculationDiagnosticSeverity.Error,
                    "Standards.ProfileShape.NegativeValue",
                    $"Profile contains negative value at index {index}.",
                    source));
            }
        }

        return new AnnualProfileShapeValidationResult(
            IsValid: diagnostics.Count == 0,
            ExpectedCount: expectedCount,
            ActualCount: values.Count,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateDiagnostic(
        CalculationDiagnosticSeverity severity,
        string code,
        string message,
        string source) =>
        new(
            Severity: severity,
            Code: code,
            Message: message,
            Context: source,
            Source: source,
            Family: StandardCalculationFamily.InternalEngineering,
            Stage: StandardCalculationStage.ProfileExpansion);
}
