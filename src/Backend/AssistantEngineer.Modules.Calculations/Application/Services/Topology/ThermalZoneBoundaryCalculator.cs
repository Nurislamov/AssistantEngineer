using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

public sealed class ThermalZoneBoundaryCalculator : IThermalZoneBoundaryCalculator
{
    private readonly IThermalBoundaryConditionResolver _boundaryConditionResolver;
    private readonly IThermalTopologyValidator _topologyValidator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public ThermalZoneBoundaryCalculator(
        IThermalBoundaryConditionResolver boundaryConditionResolver,
        IThermalTopologyValidator topologyValidator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _boundaryConditionResolver = boundaryConditionResolver ?? throw new ArgumentNullException(nameof(boundaryConditionResolver));
        _topologyValidator = topologyValidator ?? throw new ArgumentNullException(nameof(topologyValidator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public BuildingThermalBoundaryCalculationResult Calculate(ThermalZoneBoundaryCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Topology);

        var topology = input.Topology;
        var diagnostics = new List<StandardCalculationDiagnostic>();

        var validation = _topologyValidator.Validate(topology);
        diagnostics.AddRange(validation.Diagnostics);
        if (!validation.IsValid)
        {
            diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                CalculationDiagnosticSeverity.Warning,
                "AE-ZONES-TOPOLOGY-INVALID",
                "Thermal topology validation reported diagnostics; boundary calculation continues with best-effort deterministic aggregation.",
                StandardCalculationStage.Diagnostics));
        }

        var zoneTemperatures = input.ZoneAirTemperaturesCelsius is null
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : new Dictionary<string, double>(input.ZoneAirTemperaturesCelsius, StringComparer.Ordinal);
        var adjacentUnconditionedTemperatures = input.AdjacentUnconditionedTemperaturesCelsius is null
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : new Dictionary<string, double>(input.AdjacentUnconditionedTemperaturesCelsius, StringComparer.Ordinal);

        var roomsById = topology.Rooms
            .Where(room => !string.IsNullOrWhiteSpace(room.RoomId))
            .GroupBy(room => room.RoomId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var surfaceResults = topology.Surfaces
            .Select(surface => CalculateSurfaceResult(
                surface,
                topology,
                roomsById,
                zoneTemperatures,
                adjacentUnconditionedTemperatures,
                input.OutdoorTemperatureCelsius,
                input.GroundTemperatureCelsius))
            .ToArray();

        diagnostics.AddRange(surfaceResults.SelectMany(result => result.Diagnostics));

        var roomResults = ThermalZoneBoundaryResultAssembler.BuildRoomResults(topology.Rooms, surfaceResults);

        var zoneResults = ThermalZoneBoundaryResultAssembler.BuildZoneResults(
            topology.Zones,
            roomResults,
            surfaceResults);

        var unassignedRooms = roomResults
            .Where(room => string.IsNullOrWhiteSpace(room.ZoneId))
            .ToArray();

        var unassignedSurfaces = surfaceResults
            .Where(surface => string.IsNullOrWhiteSpace(surface.RoomId) && string.IsNullOrWhiteSpace(surface.ZoneId))
            .ToArray();

        var buildingTotals = ThermalZoneBoundaryAggregation.AggregateBuildingTotals(
            zoneResults,
            unassignedRooms,
            unassignedSurfaces);

        var baseDisclosure = _disclosureFactory.CreateThermalZonesDisclosure();
        var disclosure = MergeDisclosure(baseDisclosure, input.DisclosureOverride, diagnostics);

        return new BuildingThermalBoundaryCalculationResult(
            BuildingId: topology.BuildingId,
            TotalHeatTransferCoefficientWPerKelvin: buildingTotals.TotalHeatTransferCoefficientWPerKelvin,
            OutdoorHeatTransferCoefficientWPerKelvin: buildingTotals.OutdoorHeatTransferCoefficientWPerKelvin,
            GroundHeatTransferCoefficientWPerKelvin: buildingTotals.GroundHeatTransferCoefficientWPerKelvin,
            AdjacentConditionedHeatTransferCoefficientWPerKelvin: buildingTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin,
            AdjacentUnconditionedHeatTransferCoefficientWPerKelvin: buildingTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin,
            InternalPartitionHeatTransferCoefficientWPerKelvin: buildingTotals.InternalPartitionHeatTransferCoefficientWPerKelvin,
            AdiabaticAreaSquareMeters: buildingTotals.AdiabaticAreaSquareMeters,
            Zones: zoneResults,
            UnassignedRooms: unassignedRooms,
            UnassignedSurfaces: unassignedSurfaces,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private ThermalSurfaceBoundaryCalculationResult CalculateSurfaceResult(
        ThermalTopologySurface surface,
        BuildingThermalTopology topology,
        IReadOnlyDictionary<string, ThermalTopologyRoom> roomsById,
        IReadOnlyDictionary<string, double> zoneTemperatures,
        IReadOnlyDictionary<string, double> adjacentUnconditionedTemperatures,
        double? outdoorTemperatureCelsius,
        double? groundTemperatureCelsius)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(surface.Diagnostics);

        var resolution = _boundaryConditionResolver.Resolve(surface, topology);
        diagnostics.AddRange(resolution.Diagnostics);

        var classification = ThermalZoneBoundaryClassifier.Classify(
            surface,
            roomsById,
            zoneTemperatures,
            adjacentUnconditionedTemperatures,
            outdoorTemperatureCelsius,
            groundTemperatureCelsius,
            resolution.IsResolved);

        diagnostics.AddRange(classification.Diagnostics);

        var isResolved = classification.IsResolved;
        double? heatTransferCoefficient = null;

        if (!(surface.AreaSquareMeters > 0.0))
        {
            diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                CalculationDiagnosticSeverity.Error,
                "AE-ZONES-SURFACE-AREA-NONPOSITIVE",
                $"Surface '{surface.SurfaceId}' area must be greater than zero.",
                StandardCalculationStage.HeatTransfer));
            isResolved = false;
        }

        if (surface.BoundaryKind == ThermalBoundaryKind.Adiabatic)
        {
            heatTransferCoefficient = 0.0;
        }
        else if (resolution.IsHeatTransferBoundary)
        {
            if (!surface.UValueWPerSquareMeterKelvin.HasValue)
            {
                diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                    CalculationDiagnosticSeverity.Warning,
                    "AE-ZONES-SURFACE-UVALUE-MISSING",
                    $"Surface '{surface.SurfaceId}' requires a U-value for heat transfer coefficient calculation.",
                    StandardCalculationStage.HeatTransfer));
                isResolved = false;
            }
            else if (!(surface.UValueWPerSquareMeterKelvin.Value > 0.0))
            {
                diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                    CalculationDiagnosticSeverity.Error,
                    "AE-ZONES-SURFACE-UVALUE-NONPOSITIVE",
                    $"Surface '{surface.SurfaceId}' U-value must be greater than zero.",
                    StandardCalculationStage.HeatTransfer));
                isResolved = false;
            }
            else if (surface.AreaSquareMeters > 0.0)
            {
                heatTransferCoefficient = surface.AreaSquareMeters * surface.UValueWPerSquareMeterKelvin.Value;
            }
        }

        return new ThermalSurfaceBoundaryCalculationResult(
            SurfaceId: surface.SurfaceId,
            RoomId: surface.RoomId,
            ZoneId: classification.EffectiveZoneId,
            BoundaryKind: surface.BoundaryKind,
            AreaSquareMeters: surface.AreaSquareMeters,
            UValueWPerSquareMeterKelvin: surface.UValueWPerSquareMeterKelvin,
            HeatTransferCoefficientWPerKelvin: heatTransferCoefficient,
            BoundaryTemperatureCelsius: classification.BoundaryTemperatureCelsius,
            SourceZoneTemperatureCelsius: classification.SourceZoneTemperatureCelsius,
            AdjacentTemperatureCelsius: classification.AdjacentTemperatureCelsius,
            IsHeatTransferBoundary: resolution.IsHeatTransferBoundary,
            IsAdiabatic: surface.BoundaryKind == ThermalBoundaryKind.Adiabatic || resolution.IsAdiabatic,
            IsResolved: isResolved,
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
        foreach (var requiredClaim in baseBoundary.ForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(requiredClaim, StringComparer.Ordinal))
                forbiddenClaims.Add(requiredClaim);
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
            diagnostics.Add(ThermalZoneBoundaryDiagnosticsBuilder.Create(
                CalculationDiagnosticSeverity.Warning,
                "AE-ZONES-DISCLOSURE-OVERRIDE-SANITIZED",
                $"Disclosure override removed forbidden allowed-claim entries: {string.Join(", ", removedAllowedClaims)}.",
                StandardCalculationStage.Diagnostics));
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
}
