using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineDiagnosticsPolicy
{
    public Result<TTarget>? TryMapRoomLoadFailureOrValidation<TTarget>(
        Result<RoomLoadCalculationResult> roomLoad)
    {
        if (roomLoad.IsFailure)
            return Result<TTarget>.Failure(roomLoad);

        return roomLoad.Value.HasErrors
            ? Result<TTarget>.Validation(FormatErrorDiagnostics(roomLoad.Value.Diagnostics))
            : null;
    }

    public Result<TTarget>? TryMapAggregationFailureOrValidation<TTarget>(
        Result<LoadAggregationResult> aggregation)
    {
        if (aggregation.IsFailure)
            return Result<TTarget>.Failure(aggregation);

        return aggregation.Value.HasErrors
            ? Result<TTarget>.Validation(FormatErrorDiagnostics(aggregation.Value.Diagnostics))
            : null;
    }

    private static string FormatErrorDiagnostics(IEnumerable<CalculationDiagnostic> diagnostics) =>
        string.Join("; ", diagnostics
            .Where(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
}
