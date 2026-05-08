using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationControlledAirflowInputBuilder : INaturalVentilationControlledAirflowInputBuilder
{
    public NaturalVentilationCalculationInput BuildHourlyAirflowInput(
        NaturalVentilationCalculationInput baseInput,
        IReadOnlyList<NaturalVentilationOpeningOperationResult> operationsForHour)
    {
        ArgumentNullException.ThrowIfNull(baseInput);
        ArgumentNullException.ThrowIfNull(baseInput.Openings);
        ArgumentNullException.ThrowIfNull(baseInput.Environment);
        ArgumentNullException.ThrowIfNull(operationsForHour);

        var diagnostics = new List<StandardCalculationDiagnostic>(baseInput.Environment.Diagnostics);
        var operationByOpeningId = operationsForHour
            .Where(operation => !string.IsNullOrWhiteSpace(operation.OpeningId))
            .GroupBy(operation => operation.OpeningId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(operation => operation.OpeningFraction).First(),
                StringComparer.Ordinal);

        var matchedOpeningIds = new HashSet<string>(StringComparer.Ordinal);
        var openings = baseInput.Openings
            .Select(opening =>
            {
                if (operationByOpeningId.TryGetValue(opening.OpeningId, out var operation))
                {
                    matchedOpeningIds.Add(opening.OpeningId);
                    return opening with
                    {
                        OpeningFraction = operation.OpeningFraction
                    };
                }

                return opening;
            })
            .ToArray();

        foreach (var operation in operationsForHour.Where(operation => !string.IsNullOrWhiteSpace(operation.OpeningId)))
        {
            if (!matchedOpeningIds.Contains(operation.OpeningId!))
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-OPENING-NOT-FOUND",
                    $"Control operation opening id '{operation.OpeningId}' was not found in airflow input openings."));
            }
        }

        return baseInput with
        {
            Openings = openings,
            Environment = baseInput.Environment with
            {
                Diagnostics = diagnostics
            }
        };
    }

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationControlledAirflowInputBuilder");
}
