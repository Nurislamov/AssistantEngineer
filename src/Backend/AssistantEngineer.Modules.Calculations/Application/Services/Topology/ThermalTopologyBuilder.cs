using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

public sealed class ThermalTopologyBuilder : IThermalTopologyBuilder
{
    private static readonly StringComparer IdComparer = StringComparer.Ordinal;

    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public ThermalTopologyBuilder(
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public BuildingThermalTopology Build(ThermalTopologyBuildInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var zonesInput = input.Zones ?? [];
        var roomsInput = input.Rooms ?? [];
        var surfacesInput = input.Surfaces ?? [];

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var zoneIdSet = zonesInput
            .Select(zone => zone.ZoneId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(IdComparer);
        var roomIdSet = roomsInput
            .Select(room => room.RoomId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(IdComparer);

        var zones = zonesInput
            .Select(zone => BuildZone(zone, roomIdSet))
            .ToArray();
        diagnostics.AddRange(zones.SelectMany(zone => zone.Diagnostics));

        var surfaceList = surfacesInput
            .Select(surface => BuildSurface(surface, zoneIdSet, roomIdSet))
            .ToArray();
        diagnostics.AddRange(surfaceList.SelectMany(surface => surface.Diagnostics));

        var surfacesByRoomId = surfaceList
            .Where(surface => !string.IsNullOrWhiteSpace(surface.RoomId))
            .GroupBy(surface => surface.RoomId!, IdComparer)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ThermalTopologySurface>)group.ToArray(), IdComparer);

        var rooms = roomsInput
            .Select(room => BuildRoom(room, zoneIdSet, surfacesByRoomId))
            .ToArray();
        diagnostics.AddRange(rooms.SelectMany(room => room.Diagnostics));

        var baseDisclosure = _disclosureFactory.CreateThermalZonesDisclosure();
        var disclosure = MergeDisclosure(baseDisclosure, input.DisclosureOverride, diagnostics);

        return new BuildingThermalTopology(
            BuildingId: input.BuildingId ?? string.Empty,
            Zones: zones,
            Rooms: rooms,
            Surfaces: surfaceList,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private static ThermalTopologyZone BuildZone(
        ThermalTopologyZoneInput zoneInput,
        ISet<string> roomIdSet)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();
        var zoneId = zoneInput.ZoneId ?? string.Empty;
        var roomIds = zoneInput.RoomIds ?? [];

        foreach (var roomId in roomIds.Where(roomId => !string.IsNullOrWhiteSpace(roomId)))
        {
            if (!roomIdSet.Contains(roomId))
            {
                diagnostics.Add(CreateError(
                    "Topology.Builder.ZoneRoomMissing",
                    $"Zone '{zoneId}' references room '{roomId}' that does not exist in input."));
            }
        }

        return new ThermalTopologyZone(
            ZoneId: zoneId,
            Name: zoneInput.Name,
            RoomIds: roomIds,
            Diagnostics: diagnostics);
    }

    private static ThermalTopologyRoom BuildRoom(
        ThermalTopologyRoomInput roomInput,
        ISet<string> zoneIdSet,
        IReadOnlyDictionary<string, IReadOnlyList<ThermalTopologySurface>> surfacesByRoomId)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();
        var roomId = roomInput.RoomId ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(roomInput.ZoneId) && !zoneIdSet.Contains(roomInput.ZoneId))
        {
            diagnostics.Add(CreateError(
                "Topology.Builder.RoomZoneMissing",
                $"Room '{roomId}' references zone '{roomInput.ZoneId}' that does not exist in input."));
        }

        var roomSurfaces = surfacesByRoomId.TryGetValue(roomId, out var mappedSurfaces)
            ? mappedSurfaces
            : [];

        return new ThermalTopologyRoom(
            RoomId: roomId,
            ZoneId: roomInput.ZoneId,
            VolumeCubicMeters: roomInput.VolumeCubicMeters,
            FloorAreaSquareMeters: roomInput.FloorAreaSquareMeters,
            Surfaces: roomSurfaces,
            Diagnostics: diagnostics);
    }

    private static ThermalTopologySurface BuildSurface(
        ThermalTopologySurfaceInput surfaceInput,
        ISet<string> zoneIdSet,
        ISet<string> roomIdSet)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();
        var surfaceId = surfaceInput.SurfaceId ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(surfaceInput.RoomId) && !roomIdSet.Contains(surfaceInput.RoomId))
        {
            diagnostics.Add(CreateError(
                "Topology.Builder.SurfaceRoomMissing",
                $"Surface '{surfaceId}' references room '{surfaceInput.RoomId}' that does not exist in input."));
        }

        if (!string.IsNullOrWhiteSpace(surfaceInput.ZoneId) && !zoneIdSet.Contains(surfaceInput.ZoneId))
        {
            diagnostics.Add(CreateError(
                "Topology.Builder.SurfaceZoneMissing",
                $"Surface '{surfaceId}' references zone '{surfaceInput.ZoneId}' that does not exist in input."));
        }

        if (!(surfaceInput.AreaSquareMeters > 0.0))
        {
            diagnostics.Add(CreateError(
                "Topology.Builder.SurfaceAreaNonPositive",
                $"Surface '{surfaceId}' area must be greater than zero."));
        }

        if (surfaceInput.UValueWPerSquareMeterKelvin.HasValue &&
            !(surfaceInput.UValueWPerSquareMeterKelvin.Value > 0.0))
        {
            diagnostics.Add(CreateError(
                "Topology.Builder.SurfaceUValueNonPositive",
                $"Surface '{surfaceId}' U-value must be greater than zero when provided."));
        }

        if (surfaceInput.BoundaryKind == ThermalBoundaryKind.AdjacentConditionedZone &&
            string.IsNullOrWhiteSpace(surfaceInput.AdjacentZoneId) &&
            string.IsNullOrWhiteSpace(surfaceInput.AdjacentRoomId))
        {
            diagnostics.Add(CreateError(
                "Topology.Builder.AdjacentConditionedReferenceMissing",
                $"Surface '{surfaceId}' is adjacent-conditioned but has no adjacent zone/room reference."));
        }

        if (surfaceInput.BoundaryKind == ThermalBoundaryKind.AdjacentUnconditionedZone &&
            string.IsNullOrWhiteSpace(surfaceInput.BoundarySource) &&
            string.IsNullOrWhiteSpace(surfaceInput.AdjacentZoneId) &&
            string.IsNullOrWhiteSpace(surfaceInput.AdjacentRoomId))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Builder.AdjacentUnconditionedMetadataMissing",
                $"Surface '{surfaceId}' is adjacent-unconditioned but has no source/adjacent metadata."));
        }

        if (surfaceInput.BoundaryKind == ThermalBoundaryKind.Ground &&
            string.IsNullOrWhiteSpace(surfaceInput.BoundarySource))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Builder.GroundBoundarySourceMissing",
                $"Surface '{surfaceId}' is a ground boundary but has no boundary source metadata."));
        }

        if (surfaceInput.BoundaryKind == ThermalBoundaryKind.Adiabatic &&
            surfaceInput.AreaSquareMeters > 0.0 &&
            (surfaceInput.UValueWPerSquareMeterKelvin ?? 0.0) > 0.0)
        {
            diagnostics.Add(CreateWarning(
                "Topology.Builder.AdiabaticSurfaceConductanceProvided",
                $"Surface '{surfaceId}' is adiabatic but has positive area and U-value metadata."));
        }

        return new ThermalTopologySurface(
            SurfaceId: surfaceId,
            RoomId: surfaceInput.RoomId,
            ZoneId: surfaceInput.ZoneId,
            BoundaryKind: surfaceInput.BoundaryKind,
            AreaSquareMeters: surfaceInput.AreaSquareMeters,
            UValueWPerSquareMeterKelvin: surfaceInput.UValueWPerSquareMeterKelvin,
            AdjacentZoneId: surfaceInput.AdjacentZoneId,
            AdjacentRoomId: surfaceInput.AdjacentRoomId,
            BoundarySource: surfaceInput.BoundarySource,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDisclosure MergeDisclosure(
        StandardCalculationDisclosure baseDisclosure,
        StandardCalculationDisclosure? disclosureOverride,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (disclosureOverride is null)
            return baseDisclosure;

        var baseBoundary = baseDisclosure.ClaimBoundary;
        var overrideBoundary = disclosureOverride.ClaimBoundary ?? baseBoundary;

        var forbiddenClaims = overrideBoundary.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var requiredForbiddenClaim in baseBoundary.ForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(requiredForbiddenClaim, StringComparer.Ordinal))
                forbiddenClaims.Add(requiredForbiddenClaim);
        }

        var removedAllowedClaims = new List<string>();
        var allowedClaims = (overrideBoundary.AllowedClaims ?? [])
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim =>
            {
                var isForbidden = forbiddenClaims.Contains(claim, StringComparer.Ordinal);
                if (isForbidden)
                    removedAllowedClaims.Add(claim);

                return !isForbidden;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (removedAllowedClaims.Count > 0)
        {
            diagnostics.Add(CreateWarning(
                "Topology.Builder.ClaimBoundaryAllowedClaimsSanitized",
                $"Disclosure override removed forbidden items from allowed claims: {string.Join(", ", removedAllowedClaims)}."));
        }

        if (string.IsNullOrWhiteSpace(disclosureOverride.CalculationPath))
        {
            diagnostics.Add(CreateWarning(
                "Topology.Builder.DisclosurePathFallbackApplied",
                "Disclosure override calculation path was empty and fallback path was applied."));
        }

        var mergedBoundary = new StandardClaimBoundary(
            AllowedClaims: allowedClaims,
            ForbiddenClaims: forbiddenClaims,
            Limitations: overrideBoundary.Limitations ?? baseBoundary.Limitations,
            Assumptions: overrideBoundary.Assumptions ?? baseBoundary.Assumptions);

        return disclosureOverride with
        {
            CalculationPath = string.IsNullOrWhiteSpace(disclosureOverride.CalculationPath)
                ? baseDisclosure.CalculationPath
                : disclosureOverride.CalculationPath,
            ClaimBoundary = mergedBoundary
        };
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Error,
            code: code,
            message: message,
            context: "ThermalTopologyBuilder",
            stage: StandardCalculationStage.Foundation);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Warning,
            code: code,
            message: message,
            context: "ThermalTopologyBuilder",
            stage: StandardCalculationStage.Foundation);
}
