using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;

public sealed class LoadAggregationEngine
{
    private const string DesignPointMethod = "Energy Calculation equivalence / Design Point Load Aggregation";
    private const string HourlyMethod = "Energy Calculation equivalence / Hourly Coincident Load Aggregation";

    private readonly TimeProvider _timeProvider;

    public LoadAggregationEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Result<LoadAggregationResult> Aggregate(LoadAggregationInput input)
    {
        if (input is null)
            return Result<LoadAggregationResult>.Validation("Load aggregation input is required.");

        var diagnostics = new List<CalculationDiagnostic>();

        if (input.Rooms is null)
            return Result<LoadAggregationResult>.Validation("Aggregation rooms are required.");

        var rooms = SelectTargetRooms(input)
            .GroupBy(room => room.RoomId)
            .Select(group => group.First())
            .OrderBy(room => room.RoomId)
            .ToArray();

        if (rooms.Length == 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "Aggregation.NoRooms",
                "No rooms were supplied for load aggregation.",
                input.DiagnosticsContext));
        }

        foreach (var room in rooms)
        {
            if (room.AreaM2 < 0)
            {
                diagnostics.Add(Error(
                    "Aggregation.InvalidRoomArea",
                    $"Room {room.RoomId} area must not be negative.",
                    input.DiagnosticsContext));
            }
        }

        var useHourly = input.Mode == LoadAggregationMode.Hourly &&
            rooms.Length > 0 &&
            rooms.All(room => room.HourlyHeatingLoadW is { Count: > 0 } && room.HourlyCoolingLoadW is { Count: > 0 });

        var hourlyRecordCountUsed = useHourly
            ? HourlyRecordCountUsed(rooms)
            : (int?)null;

        if (useHourly && HasHourlyProfileLengthMismatch(rooms))
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "Aggregation.HourlyProfileLengthMismatch",
                "Hourly aggregation profiles have different lengths; coincident aggregation used the shortest available common profile length.",
                input.DiagnosticsContext));
        }

        if (input.Mode == LoadAggregationMode.Hourly && !useHourly)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "Aggregation.HourlyUnavailable",
                "Hourly aggregation is not available; design-point aggregation was used.",
                input.DiagnosticsContext));
        }

        if (HasErrorDiagnostics(diagnostics))
        {
            return Result<LoadAggregationResult>.Validation(
                BuildValidationFailureMessage(
                    "Load aggregation validation failed",
                    diagnostics));
        }

        var heatingLoadW = useHourly
            ? PeakCoincident(rooms.Select(room => room.HourlyHeatingLoadW!).ToArray())
            : rooms.Sum(room => Math.Max(0, room.HeatingLoadW));

        var coolingLoadW = useHourly
            ? PeakCoincident(rooms.Select(room => room.HourlyCoolingLoadW!).ToArray())
            : rooms.Sum(room => Math.Max(0, room.CoolingLoadW));

        var area = rooms.Sum(room => Math.Max(0, room.AreaM2));
        var components = SumComponents(rooms);

        var roomBreakdown = rooms
            .Select(room => new LoadAggregationRoomBreakdown(
                room.RoomId,
                room.RoomName,
                Round(room.AreaM2),
                Round(Math.Max(0, room.HeatingLoadW)),
                Round(Math.Max(0, room.CoolingLoadW))))
            .ToArray();

        return Result<LoadAggregationResult>.Success(new LoadAggregationResult(
            input.TargetId,
            input.TargetType,
            input.TargetName,
            rooms.Length,
            Round(area),
            Round(Math.Max(0, heatingLoadW)),
            Round(Math.Max(0, coolingLoadW)),
            area > 0 ? Round(heatingLoadW / area) : 0,
            area > 0 ? Round(coolingLoadW / area) : 0,
            roomBreakdown,
            components,
            useHourly ? HourlyMethod : DesignPointMethod,
            diagnostics,
            _timeProvider.GetUtcNow(),
            hourlyRecordCountUsed));
    }

    private static IEnumerable<AggregationRoomLoadInput> SelectTargetRooms(LoadAggregationInput input) =>
        input.TargetType switch
        {
            LoadAggregationTargetType.ThermalZone => input.Rooms.Where(room => room.ThermalZoneId == input.TargetId),
            LoadAggregationTargetType.Floor => input.Rooms.Where(room => room.FloorId == input.TargetId),
            LoadAggregationTargetType.Building => input.Rooms.Where(room => room.BuildingId == input.TargetId),
            _ => []
        };

    private static int HourlyRecordCountUsed(IReadOnlyList<AggregationRoomLoadInput> rooms)
    {
        var counts = rooms
            .SelectMany(room => new[]
            {
                room.HourlyHeatingLoadW?.Count ?? 0,
                room.HourlyCoolingLoadW?.Count ?? 0
            })
            .Where(count => count > 0)
            .ToArray();

        return counts.Length == 0 ? 0 : counts.Min();
    }

    private static bool HasHourlyProfileLengthMismatch(IReadOnlyList<AggregationRoomLoadInput> rooms)
    {
        var counts = rooms
            .SelectMany(room => new[]
            {
                room.HourlyHeatingLoadW?.Count ?? 0,
                room.HourlyCoolingLoadW?.Count ?? 0
            })
            .Where(count => count > 0)
            .Distinct()
            .ToArray();

        return counts.Length > 1;
    }

    private static double PeakCoincident(IReadOnlyList<IReadOnlyList<double>> profiles)
    {
        var hourCount = profiles.Min(profile => profile.Count);
        if (hourCount == 0)
            return 0;

        var peak = 0.0;

        for (var hour = 0; hour < hourCount; hour++)
        {
            var sum = profiles.Sum(profile => Math.Max(0, profile[hour]));
            if (sum > peak)
                peak = sum;
        }

        return peak;
    }

    private static AggregationComponentBreakdown SumComponents(IReadOnlyList<AggregationRoomLoadInput> rooms)
    {
        var transmission =
            rooms.Sum(room => room.HeatingBreakdown?.TransmissionW ?? 0) +
            rooms.Sum(room => room.HeatingBreakdown?.WindowTransmissionW ?? 0) +
            rooms.Sum(room => room.CoolingBreakdown?.TransmissionW ?? 0) +
            rooms.Sum(room => room.CoolingBreakdown?.WindowTransmissionW ?? 0);

        var solar = rooms.Sum(room => room.CoolingBreakdown?.SolarW ?? 0);

        var ventilation =
            rooms.Sum(room => room.HeatingBreakdown?.VentilationW ?? 0) +
            rooms.Sum(room => room.CoolingBreakdown?.VentilationW ?? 0);

        var infiltration =
            rooms.Sum(room => room.HeatingBreakdown?.InfiltrationW ?? 0) +
            rooms.Sum(room => room.CoolingBreakdown?.InfiltrationW ?? 0);

        var internalGains = rooms.Sum(room => room.CoolingBreakdown?.InternalGainsW ?? 0);

        var ground =
            rooms.Sum(room => room.HeatingBreakdown?.GroundW ?? 0) +
            rooms.Sum(room => room.CoolingBreakdown?.GroundW ?? 0);

        return new AggregationComponentBreakdown(
            Round(transmission),
            Round(solar),
            Round(ventilation),
            Round(infiltration),
            Round(internalGains),
            Round(ground));
    }

    private static CalculationDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            context);

    private static bool HasErrorDiagnostics(
        IEnumerable<CalculationDiagnostic> diagnostics) =>
        diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);

    private static string BuildValidationFailureMessage(
        string prefix,
        IEnumerable<CalculationDiagnostic> diagnostics)
    {
        var errorCodes = diagnostics
            .Where(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return errorCodes.Length == 0
            ? prefix + "."
            : $"{prefix}: {string.Join(", ", errorCodes)}.";
    }

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
