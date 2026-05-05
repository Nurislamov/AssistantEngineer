using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

public sealed class Iso52016PhysicalRoomModelDiagnosticsBuilder : IIso52016PhysicalRoomModelDiagnosticsBuilder
{
    private readonly IIso52016PhysicalRoomModelBuilder _physicalRoomModelBuilder;

    public Iso52016PhysicalRoomModelDiagnosticsBuilder(
        IIso52016PhysicalRoomModelBuilder physicalRoomModelBuilder)
    {
        _physicalRoomModelBuilder = physicalRoomModelBuilder;
    }

    public Result<Iso52016PhysicalRoomModelDiagnosticsProfile> Build(
        Iso52016PhysicalRoomModelRequest request)
    {
        if (request is null)
            return Result<Iso52016PhysicalRoomModelDiagnosticsProfile>.Validation(
                "ISO 52016 physical room model diagnostics request is required.");

        var matrixRequestResult = _physicalRoomModelBuilder.Build(request);

        if (matrixRequestResult.IsFailure)
            return Result<Iso52016PhysicalRoomModelDiagnosticsProfile>.Failure(matrixRequestResult);

        var matrixRequest = matrixRequestResult.Value;
        var sourceHoursByHourOfYear = request.HourlyInputProfile?.Hours?
            .ToDictionary(hour => hour.HourOfYear);

        var hourlyDiagnostics = matrixRequest.Hours
            .Select(hour => BuildHourlyDiagnostics(hour, sourceHoursByHourOfYear))
            .ToArray();

        var boundaryIds = matrixRequest.BoundaryConductances
            .Select(link => link.BoundaryId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var profile = new Iso52016PhysicalRoomModelDiagnosticsProfile(
            ZoneCode: matrixRequest.ZoneCode,
            AirNodeId: matrixRequest.Options?.AirNodeId ?? string.Empty,
            NodeCount: matrixRequest.Nodes.Count,
            InternalConductanceLinkCount: matrixRequest.InternalConductances.Count,
            BoundaryConductanceLinkCount: matrixRequest.BoundaryConductances.Count,
            HourCount: matrixRequest.Hours.Count,
            TotalHeatCapacityJPerK: matrixRequest.Nodes.Sum(node => node.HeatCapacityJPerK),
            TotalInternalConductanceWPerK: matrixRequest.InternalConductances.Sum(link => link.ConductanceWPerK),
            TotalBoundaryConductanceWPerK: matrixRequest.BoundaryConductances.Sum(link => link.ConductanceWPerK),
            NodeIds: matrixRequest.Nodes.Select(node => node.NodeId).ToArray(),
            BoundaryIds: boundaryIds,
            Hours: hourlyDiagnostics);

        return Result<Iso52016PhysicalRoomModelDiagnosticsProfile>.Success(profile);
    }

    private static Iso52016PhysicalRoomModelHourlyDiagnostics BuildHourlyDiagnostics(
        Iso52016MatrixHourlyInputRecord hour,
        IReadOnlyDictionary<int, Contracts.Iso52016.Iso52016RoomHourlyInputRecord>? sourceHoursByHourOfYear)
    {
        var distributedNodeHeatGainsW = hour.NodeHeatGainsW.Values.Sum();

        var sourceTotalGainsW = sourceHoursByHourOfYear is not null &&
            sourceHoursByHourOfYear.TryGetValue(hour.HourOfYear, out var sourceHour)
                ? sourceHour.TotalGainsW
                : distributedNodeHeatGainsW;

        var overrides = hour.BoundaryConductanceOverrides ??
            Array.Empty<Iso52016MatrixHourlyBoundaryConductanceOverride>();

        return new Iso52016PhysicalRoomModelHourlyDiagnostics(
            HourOfYear: hour.HourOfYear,
            Month: hour.Month,
            Day: hour.Day,
            Hour: hour.Hour,
            SourceTotalGainsW: sourceTotalGainsW,
            DistributedNodeHeatGainsW: distributedNodeHeatGainsW,
            NodeGainBalanceErrorW: distributedNodeHeatGainsW - sourceTotalGainsW,
            BoundaryConductanceOverrideCount: overrides.Count,
            MaxBoundaryConductanceOverrideWPerK: overrides.Count == 0
                ? 0.0
                : overrides.Max(overrideRecord => overrideRecord.ConductanceWPerK));
    }
}
