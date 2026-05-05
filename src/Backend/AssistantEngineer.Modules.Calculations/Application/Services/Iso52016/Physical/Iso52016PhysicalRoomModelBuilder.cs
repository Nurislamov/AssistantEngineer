using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

public sealed class Iso52016PhysicalRoomModelBuilder : IIso52016PhysicalRoomModelBuilder
{
    private const double FractionTolerance = 1e-9;

    public Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016PhysicalRoomModelRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016MatrixHourlySolverRequest>.Failure(validation);

        var hourlyInputProfile = request.HourlyInputProfile;
        var heatBalanceOptions = request.HeatBalanceOptions ?? new Iso52016RoomHeatBalanceOptions();
        var modelOptions = request.ModelOptions ?? new Iso52016PhysicalNodeModelOptions();
        var surfaces = (request.Surfaces ?? Array.Empty<Iso52016PhysicalSurface>()).ToArray();
        var surfaceBoundaryConditions = (request.SurfaceBoundaryConditions ?? Array.Empty<Iso52016PhysicalSurfaceHourlyBoundaryCondition>()).ToArray();
        var operationConditions = (request.OperationConditions ?? Array.Empty<Iso52016PhysicalHourlyOperationCondition>()).ToArray();
        var operationConditionsByHour = BuildOperationConditionLookup(operationConditions);

        if (surfaces.Length == 0)
        {
            return BuildAggregatedThreeNodeRequest(
                hourlyInputProfile,
                heatBalanceOptions,
                modelOptions,
                operationConditionsByHour);
        }

        return BuildSurfaceExpandedRequest(
            hourlyInputProfile,
            heatBalanceOptions,
            modelOptions,
            surfaces,
            surfaceBoundaryConditions,
            operationConditionsByHour);
    }

    private static Result<Iso52016MatrixHourlySolverRequest> BuildAggregatedThreeNodeRequest(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016RoomHeatBalanceOptions heatBalanceOptions,
        Iso52016PhysicalNodeModelOptions modelOptions,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour)
    {
        var airNodeId = modelOptions.AirNodeId.Trim();
        var internalSurfaceNodeId = modelOptions.InternalSurfaceNodeId.Trim();
        var thermalMassNodeId = modelOptions.ThermalMassNodeId.Trim();

        var outdoorBoundaryId = modelOptions.OutdoorBoundaryId.Trim();
        var groundBoundaryId = modelOptions.GroundBoundaryId.Trim();
        var adjacentBoundaryId = modelOptions.AdjacentBoundaryId.Trim();
        var ventilationBoundaryId = modelOptions.VentilationBoundaryId.Trim();

        var initialTemperatureC = heatBalanceOptions.InitialIndoorTemperatureC;
        var totalCapacityJPerK = hourlyInputProfile.ThermalCapacityJPerK;
        var ventilationConductanceByHour = BuildVentilationConductanceByHour(
            hourlyInputProfile,
            operationConditionsByHour);
        var hasVentilation = ventilationConductanceByHour.Values.Any(value => value > 0);

        var nodes = new[]
        {
            new Iso52016MatrixNodeDefinition(
                NodeId: airNodeId,
                HeatCapacityJPerK: totalCapacityJPerK * modelOptions.AirHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC,
                IsAirNode: true),

            new Iso52016MatrixNodeDefinition(
                NodeId: internalSurfaceNodeId,
                HeatCapacityJPerK: totalCapacityJPerK * modelOptions.InternalSurfaceHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC),

            new Iso52016MatrixNodeDefinition(
                NodeId: thermalMassNodeId,
                HeatCapacityJPerK: totalCapacityJPerK * modelOptions.ThermalMassHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC)
        };

        var airToSurfaceConductanceWPerK =
            modelOptions.AirToInternalSurfaceConductanceWPerK ??
            hourlyInputProfile.TotalHeatTransferCoefficientWPerK *
            modelOptions.AirToInternalSurfaceConductanceMultiplier;

        var surfaceToMassConductanceWPerK =
            modelOptions.InternalSurfaceToThermalMassConductanceWPerK ??
            hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK *
            modelOptions.InternalSurfaceToThermalMassConductanceMultiplier;

        var internalConductances = new[]
        {
            new Iso52016MatrixConductanceLink(
                FromNodeId: airNodeId,
                ToNodeId: internalSurfaceNodeId,
                ConductanceWPerK: airToSurfaceConductanceWPerK),

            new Iso52016MatrixConductanceLink(
                FromNodeId: internalSurfaceNodeId,
                ToNodeId: thermalMassNodeId,
                ConductanceWPerK: surfaceToMassConductanceWPerK)
        };

        var boundaryConductances = new List<Iso52016MatrixBoundaryConductance>
        {
            new(
                NodeId: internalSurfaceNodeId,
                BoundaryId: outdoorBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK *
                    modelOptions.OutdoorTransmissionConductanceFraction),

            new(
                NodeId: internalSurfaceNodeId,
                BoundaryId: groundBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK *
                    modelOptions.GroundTransmissionConductanceFraction),

            new(
                NodeId: internalSurfaceNodeId,
                BoundaryId: adjacentBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK *
                    modelOptions.AdjacentTransmissionConductanceFraction)
        };

        if (hasVentilation)
        {
            boundaryConductances.Add(
                new Iso52016MatrixBoundaryConductance(
                    NodeId: airNodeId,
                    BoundaryId: ventilationBoundaryId,
                    ConductanceWPerK: ventilationConductanceByHour.Values.Max()));
        }

        var hours = hourlyInputProfile.Hours
            .Select(hour =>
            {
                var internalConvectiveFraction = ResolveInternalGainsConvectiveFraction(
                    modelOptions,
                    hour,
                    operationConditionsByHour);
                var solarToAirFraction = ResolveSolarGainsToAirFraction(
                    hour,
                    operationConditionsByHour);
                var ventilationConductanceWPerK = ventilationConductanceByHour[hour.HourOfYear];

                var internalConvectiveGainsW = hour.InternalGainsW * internalConvectiveFraction;
                var internalRadiativeGainsW = hour.InternalGainsW - internalConvectiveGainsW;
                var solarAirGainsW = hour.SolarGainsW * solarToAirFraction;
                var remainingSolarGainsW = hour.SolarGainsW - solarAirGainsW;

                var internalSurfaceGainsW =
                    remainingSolarGainsW * modelOptions.SolarGainsToInternalSurfaceFraction +
                    internalRadiativeGainsW * modelOptions.InternalRadiativeGainsToInternalSurfaceFraction;

                var thermalMassGainsW =
                    remainingSolarGainsW * (1.0 - modelOptions.SolarGainsToInternalSurfaceFraction) +
                    internalRadiativeGainsW * (1.0 - modelOptions.InternalRadiativeGainsToInternalSurfaceFraction);

                var boundaryTemperatures = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    [outdoorBoundaryId] = hour.OutdoorTemperatureC,
                    [groundBoundaryId] = hour.GroundBoundaryTemperatureC,
                    [adjacentBoundaryId] = modelOptions.AdjacentBoundaryTemperatureC
                };

                if (hasVentilation)
                {
                    boundaryTemperatures[ventilationBoundaryId] = ResolveVentilationBoundaryTemperatureC(
                        hour,
                        operationConditionsByHour);
                }

                return new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    BoundaryTemperaturesC: boundaryTemperatures,
                    NodeHeatGainsW: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        [airNodeId] = internalConvectiveGainsW + solarAirGainsW,
                        [internalSurfaceNodeId] = internalSurfaceGainsW,
                        [thermalMassNodeId] = thermalMassGainsW
                    },
                    HeatingSetpointC: hour.HeatingSetpointC,
                    CoolingSetpointC: hour.CoolingSetpointC,
                    BoundaryConductanceOverrides: hasVentilation
                        ? new[]
                        {
                            new Iso52016MatrixHourlyBoundaryConductanceOverride(
                                NodeId: airNodeId,
                                BoundaryId: ventilationBoundaryId,
                                ConductanceWPerK: ventilationConductanceWPerK)
                        }
                        : null);
            })
            .ToArray();

        return CreateSuccessRequest(
            hourlyInputProfile,
            heatBalanceOptions,
            airNodeId,
            nodes,
            internalConductances,
            boundaryConductances,
            hours);
    }

    private static Result<Iso52016MatrixHourlySolverRequest> BuildSurfaceExpandedRequest(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016RoomHeatBalanceOptions heatBalanceOptions,
        Iso52016PhysicalNodeModelOptions modelOptions,
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> surfaceBoundaryConditions,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour)
    {
        var airNodeId = modelOptions.AirNodeId.Trim();
        var initialTemperatureC = heatBalanceOptions.InitialIndoorTemperatureC;
        var ventilationConductanceByHour = BuildVentilationConductanceByHour(
            hourlyInputProfile,
            operationConditionsByHour);
        var hasVentilation = ventilationConductanceByHour.Values.Any(value => value > 0);

        var nodes = new List<Iso52016MatrixNodeDefinition>
        {
            new(
                NodeId: airNodeId,
                HeatCapacityJPerK: hourlyInputProfile.ThermalCapacityJPerK * modelOptions.AirHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC,
                IsAirNode: true)
        };

        var internalConductances = new List<Iso52016MatrixConductanceLink>();
        var boundaryConductances = new List<Iso52016MatrixBoundaryConductance>();
        var boundaryConditionsBySurface = BuildBoundaryConditionLookup(surfaceBoundaryConditions);
        var surfacesWithHourlyBoundaryConditions = new HashSet<string>(
            boundaryConditionsBySurface.Keys,
            StringComparer.OrdinalIgnoreCase);

        foreach (var surface in surfaces)
        {
            var surfaceNodeId = ResolveSurfaceNodeId(surface);
            var massNodeId = ResolveMassNodeId(surface);
            var constructionCapacityJPerK = CalculateConstructionHeatCapacityJPerK(surface);
            var surfaceHeatCapacityJPerK = surface.HeatCapacityJPerK ??
                Math.Max(
                    constructionCapacityJPerK * modelOptions.SurfaceNodeHeatCapacityFraction,
                    modelOptions.MinimumSurfaceNodeHeatCapacityJPerK);
            var massHeatCapacityJPerK = surface.MassHeatCapacityJPerK ??
                Math.Max(
                    constructionCapacityJPerK - surfaceHeatCapacityJPerK,
                    modelOptions.MinimumMassNodeHeatCapacityJPerK);
            var boundaryConductanceWPerK = surface.BoundaryConductanceWPerK ??
                CalculateBoundaryConductanceWPerK(surface);
            var surfaceToAirConductanceWPerK = surface.SurfaceToAirConductanceWPerK ??
                surface.AreaM2 * modelOptions.DefaultSurfaceToAirConductanceWPerM2K;
            var surfaceToMassConductanceWPerK = surface.SurfaceToMassConductanceWPerK ??
                boundaryConductanceWPerK * modelOptions.SurfaceToMassConductanceMultiplier;

            nodes.Add(
                new Iso52016MatrixNodeDefinition(
                    NodeId: surfaceNodeId,
                    HeatCapacityJPerK: surfaceHeatCapacityJPerK,
                    InitialTemperatureC: initialTemperatureC));

            nodes.Add(
                new Iso52016MatrixNodeDefinition(
                    NodeId: massNodeId,
                    HeatCapacityJPerK: massHeatCapacityJPerK,
                    InitialTemperatureC: initialTemperatureC));

            internalConductances.Add(
                new Iso52016MatrixConductanceLink(
                    FromNodeId: airNodeId,
                    ToNodeId: surfaceNodeId,
                    ConductanceWPerK: surfaceToAirConductanceWPerK));

            internalConductances.Add(
                new Iso52016MatrixConductanceLink(
                    FromNodeId: surfaceNodeId,
                    ToNodeId: massNodeId,
                    ConductanceWPerK: surfaceToMassConductanceWPerK));

            boundaryConductances.Add(
                new Iso52016MatrixBoundaryConductance(
                    NodeId: surfaceNodeId,
                    BoundaryId: ResolveBoundaryId(surface, modelOptions, surfacesWithHourlyBoundaryConditions),
                    ConductanceWPerK: boundaryConductanceWPerK));
        }

        if (hasVentilation)
        {
            boundaryConductances.Add(
                new Iso52016MatrixBoundaryConductance(
                    NodeId: airNodeId,
                    BoundaryId: modelOptions.VentilationBoundaryId.Trim(),
                    ConductanceWPerK: ventilationConductanceByHour.Values.Max()));
        }

        var solarDistribution = BuildSurfaceDistribution(
            surfaces,
            static surface => surface.SolarGainsDistributionFraction);

        var internalRadiativeDistribution = BuildSurfaceDistribution(
            surfaces,
            static surface => surface.InternalRadiativeGainsDistributionFraction);

        var hours = hourlyInputProfile.Hours
            .Select(hour =>
            {
                var boundaryTemperatures = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var ventilationConductanceWPerK = ventilationConductanceByHour[hour.HourOfYear];

                if (hasVentilation)
                {
                    boundaryTemperatures[modelOptions.VentilationBoundaryId.Trim()] = ResolveVentilationBoundaryTemperatureC(
                        hour,
                        operationConditionsByHour);
                }

                var internalConvectiveFraction = ResolveInternalGainsConvectiveFraction(
                    modelOptions,
                    hour,
                    operationConditionsByHour);
                var solarToAirFraction = ResolveSolarGainsToAirFraction(
                    hour,
                    operationConditionsByHour);
                var internalRadiativeGainsW =
                    hour.InternalGainsW * (1.0 - internalConvectiveFraction);
                var solarAirGainsW = hour.SolarGainsW * solarToAirFraction;
                var remainingSolarGainsW = hour.SolarGainsW - solarAirGainsW;

                var nodeHeatGains = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    [airNodeId] = hour.InternalGainsW * internalConvectiveFraction + solarAirGainsW
                };

                foreach (var surface in surfaces)
                {
                    var surfaceId = surface.SurfaceId.Trim();
                    var surfaceNodeId = ResolveSurfaceNodeId(surface);
                    var boundaryId = ResolveBoundaryId(surface, modelOptions, surfacesWithHourlyBoundaryConditions);

                    boundaryTemperatures[boundaryId] = ResolveBoundaryTemperatureC(
                        surface,
                        modelOptions,
                        hour,
                        boundaryConditionsBySurface);

                    nodeHeatGains[surfaceNodeId] =
                        remainingSolarGainsW * solarDistribution[surfaceId] +
                        internalRadiativeGainsW * internalRadiativeDistribution[surfaceId];
                }

                return new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    BoundaryTemperaturesC: boundaryTemperatures,
                    NodeHeatGainsW: nodeHeatGains,
                    HeatingSetpointC: hour.HeatingSetpointC,
                    CoolingSetpointC: hour.CoolingSetpointC,
                    BoundaryConductanceOverrides: hasVentilation
                        ? new[]
                        {
                            new Iso52016MatrixHourlyBoundaryConductanceOverride(
                                NodeId: airNodeId,
                                BoundaryId: modelOptions.VentilationBoundaryId.Trim(),
                                ConductanceWPerK: ventilationConductanceWPerK)
                        }
                        : null);
            })
            .ToArray();

        return CreateSuccessRequest(
            hourlyInputProfile,
            heatBalanceOptions,
            airNodeId,
            nodes,
            internalConductances,
            boundaryConductances,
            hours);
    }

    private static Result<Iso52016MatrixHourlySolverRequest> CreateSuccessRequest(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016RoomHeatBalanceOptions heatBalanceOptions,
        string airNodeId,
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixConductanceLink> internalConductances,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyList<Iso52016MatrixHourlyInputRecord> hours)
    {
        var solverOptions = new Iso52016MatrixHourlySolverOptions(
            TimeStepSeconds: heatBalanceOptions.TimeStepSeconds,
            AirNodeId: airNodeId,
            DefaultHeatingSetpointC: hourlyInputProfile.HeatingSetpointC,
            DefaultCoolingSetpointC: hourlyInputProfile.CoolingSetpointC);

        return Result<Iso52016MatrixHourlySolverRequest>.Success(
            new Iso52016MatrixHourlySolverRequest(
                ZoneCode: hourlyInputProfile.RoomCode.Trim(),
                Nodes: nodes,
                InternalConductances: internalConductances,
                BoundaryConductances: boundaryConductances,
                Hours: hours,
                Options: solverOptions));
    }

    private static Result Validate(
        Iso52016PhysicalRoomModelRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 physical room model request is required.");

        if (request.HourlyInputProfile is null)
            return Result.Validation("ISO 52016 physical room model requires an hourly input profile.");

        var hourlyInputProfile = request.HourlyInputProfile;

        if (string.IsNullOrWhiteSpace(hourlyInputProfile.RoomCode))
            return Result.Validation("ISO 52016 physical room model requires a room code.");

        if (hourlyInputProfile.HourCount == 0)
            return Result.Validation("ISO 52016 physical room model requires hourly records.");

        if (hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK <= 0)
            return Result.Validation("ISO 52016 physical room model requires positive transmission heat transfer coefficient.");

        if (hourlyInputProfile.VentilationHeatTransferCoefficientWPerK < 0)
            return Result.Validation("ISO 52016 physical room model ventilation heat transfer coefficient cannot be negative.");

        if (hourlyInputProfile.ThermalCapacityJPerK <= 0)
            return Result.Validation("ISO 52016 physical room model requires positive thermal capacity.");

        if (hourlyInputProfile.CoolingSetpointC <= hourlyInputProfile.HeatingSetpointC)
            return Result.Validation("ISO 52016 physical room model cooling setpoint must be greater than heating setpoint.");

        var heatBalanceOptions = request.HeatBalanceOptions ?? new Iso52016RoomHeatBalanceOptions();

        if (heatBalanceOptions.TimeStepSeconds <= 0)
            return Result.Validation("ISO 52016 physical room model time step must be greater than zero.");

        var modelOptions = request.ModelOptions ?? new Iso52016PhysicalNodeModelOptions();

        var idValidation = ValidateIds(modelOptions);

        if (idValidation.IsFailure)
            return idValidation;

        var fractionValidation = ValidateFractions(modelOptions);

        if (fractionValidation.IsFailure)
            return fractionValidation;

        var derivedConductanceValidation = ValidateDerivedConductances(
            hourlyInputProfile,
            modelOptions);

        if (derivedConductanceValidation.IsFailure)
            return derivedConductanceValidation;

        foreach (var hour in hourlyInputProfile.Hours)
        {
            if (hour.TransmissionHeatTransferCoefficientWPerK <= 0)
                return Result.Validation($"ISO 52016 physical room model requires positive transmission heat transfer coefficient at hour {hour.HourOfYear}.");

            if (hour.VentilationHeatTransferCoefficientWPerK < 0)
                return Result.Validation($"ISO 52016 physical room model ventilation heat transfer coefficient cannot be negative at hour {hour.HourOfYear}.");

            if (hour.ThermalCapacityJPerK <= 0)
                return Result.Validation($"ISO 52016 physical room model requires positive thermal capacity at hour {hour.HourOfYear}.");

            if (hour.CoolingSetpointC <= hour.HeatingSetpointC)
                return Result.Validation($"ISO 52016 physical room model cooling setpoint must be greater than heating setpoint at hour {hour.HourOfYear}.");
        }

        var surfaces = (request.Surfaces ?? Array.Empty<Iso52016PhysicalSurface>()).ToArray();
        var surfaceBoundaryConditions = (request.SurfaceBoundaryConditions ?? Array.Empty<Iso52016PhysicalSurfaceHourlyBoundaryCondition>()).ToArray();
        var operationConditions = (request.OperationConditions ?? Array.Empty<Iso52016PhysicalHourlyOperationCondition>()).ToArray();

        if (surfaces.Length > 0)
        {
            var surfaceValidation = ValidateSurfaces(
                surfaces,
                modelOptions);

            if (surfaceValidation.IsFailure)
                return surfaceValidation;
        }

        var boundaryConditionValidation = ValidateSurfaceBoundaryConditions(
            surfaceBoundaryConditions,
            surfaces,
            hourlyInputProfile);

        if (boundaryConditionValidation.IsFailure)
            return boundaryConditionValidation;

        var operationConditionValidation = ValidateOperationConditions(
            operationConditions,
            hourlyInputProfile);

        if (operationConditionValidation.IsFailure)
            return operationConditionValidation;

        return Result.Success();
    }

    private static Result ValidateIds(
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        var nodeIds = new[]
        {
            modelOptions.AirNodeId,
            modelOptions.InternalSurfaceNodeId,
            modelOptions.ThermalMassNodeId
        };

        if (nodeIds.Any(string.IsNullOrWhiteSpace))
            return Result.Validation("ISO 52016 physical room model node ids are required.");

        if (nodeIds.Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != nodeIds.Length)
            return Result.Validation("ISO 52016 physical room model node ids must be unique.");

        var boundaryIds = new[]
        {
            modelOptions.OutdoorBoundaryId,
            modelOptions.GroundBoundaryId,
            modelOptions.AdjacentBoundaryId,
            modelOptions.VentilationBoundaryId,
            modelOptions.AdjacentConditionedBoundaryId,
            modelOptions.AdjacentUnconditionedBoundaryId
        };

        if (boundaryIds.Any(string.IsNullOrWhiteSpace))
            return Result.Validation("ISO 52016 physical room model boundary ids are required.");

        if (boundaryIds.Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != boundaryIds.Length)
            return Result.Validation("ISO 52016 physical room model boundary ids must be unique.");

        return Result.Success();
    }

    private static Result ValidateFractions(
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        foreach (var (name, value) in new[]
        {
            ("air heat capacity fraction", modelOptions.AirHeatCapacityFraction),
            ("internal surface heat capacity fraction", modelOptions.InternalSurfaceHeatCapacityFraction),
            ("thermal mass heat capacity fraction", modelOptions.ThermalMassHeatCapacityFraction),
            ("outdoor transmission conductance fraction", modelOptions.OutdoorTransmissionConductanceFraction),
            ("ground transmission conductance fraction", modelOptions.GroundTransmissionConductanceFraction),
            ("adjacent transmission conductance fraction", modelOptions.AdjacentTransmissionConductanceFraction),
            ("internal gains convective fraction", modelOptions.InternalGainsConvectiveFraction),
            ("internal radiative gains to internal surface fraction", modelOptions.InternalRadiativeGainsToInternalSurfaceFraction),
            ("solar gains to internal surface fraction", modelOptions.SolarGainsToInternalSurfaceFraction),
            ("surface node heat capacity fraction", modelOptions.SurfaceNodeHeatCapacityFraction)
        })
        {
            if (value < 0 || value > 1)
                return Result.Validation($"ISO 52016 physical room model {name} must be between 0 and 1.");
        }

        var capacityFractionSum =
            modelOptions.AirHeatCapacityFraction +
            modelOptions.InternalSurfaceHeatCapacityFraction +
            modelOptions.ThermalMassHeatCapacityFraction;

        if (Math.Abs(capacityFractionSum - 1.0) > FractionTolerance)
            return Result.Validation("ISO 52016 physical room model heat capacity fractions must sum to 1.0.");

        var boundaryFractionSum =
            modelOptions.OutdoorTransmissionConductanceFraction +
            modelOptions.GroundTransmissionConductanceFraction +
            modelOptions.AdjacentTransmissionConductanceFraction;

        if (Math.Abs(boundaryFractionSum - 1.0) > FractionTolerance)
            return Result.Validation("ISO 52016 physical room model boundary conductance fractions must sum to 1.0.");

        if (modelOptions.DefaultSurfaceToAirConductanceWPerM2K <= 0)
            return Result.Validation("ISO 52016 physical room model default surface-to-air conductance must be greater than zero.");

        if (modelOptions.SurfaceToMassConductanceMultiplier <= 0)
            return Result.Validation("ISO 52016 physical room model surface-to-mass conductance multiplier must be greater than zero.");

        if (modelOptions.MinimumSurfaceNodeHeatCapacityJPerK <= 0)
            return Result.Validation("ISO 52016 physical room model minimum surface node heat capacity must be greater than zero.");

        if (modelOptions.MinimumMassNodeHeatCapacityJPerK <= 0)
            return Result.Validation("ISO 52016 physical room model minimum mass node heat capacity must be greater than zero.");

        return Result.Success();
    }

    private static Result ValidateDerivedConductances(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        if (modelOptions.AirToInternalSurfaceConductanceMultiplier <= 0)
            return Result.Validation("ISO 52016 physical room model air-to-surface conductance multiplier must be greater than zero.");

        if (modelOptions.InternalSurfaceToThermalMassConductanceMultiplier <= 0)
            return Result.Validation("ISO 52016 physical room model surface-to-mass conductance multiplier must be greater than zero.");

        var airToSurfaceConductanceWPerK =
            modelOptions.AirToInternalSurfaceConductanceWPerK ??
            hourlyInputProfile.TotalHeatTransferCoefficientWPerK *
            modelOptions.AirToInternalSurfaceConductanceMultiplier;

        var surfaceToMassConductanceWPerK =
            modelOptions.InternalSurfaceToThermalMassConductanceWPerK ??
            hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK *
            modelOptions.InternalSurfaceToThermalMassConductanceMultiplier;

        if (airToSurfaceConductanceWPerK <= 0)
            return Result.Validation("ISO 52016 physical room model air-to-surface conductance must be greater than zero.");

        if (surfaceToMassConductanceWPerK <= 0)
            return Result.Validation("ISO 52016 physical room model surface-to-mass conductance must be greater than zero.");

        return Result.Success();
    }

    private static Result ValidateSurfaces(
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Iso52016PhysicalNodeModelOptions modelOptions)
    {
        if (surfaces.Any(surface => surface is null))
            return Result.Validation("ISO 52016 physical room model surfaces cannot contain null records.");

        if (surfaces.Any(surface => string.IsNullOrWhiteSpace(surface.SurfaceId)))
            return Result.Validation("ISO 52016 physical room model surface ids are required.");

        if (surfaces.Select(surface => surface.SurfaceId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != surfaces.Count)
            return Result.Validation("ISO 52016 physical room model surface ids must be unique.");

        var nodeIds = new List<string> { modelOptions.AirNodeId.Trim() };

        foreach (var surface in surfaces)
        {
            if (surface.AreaM2 <= 0)
                return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' area must be greater than zero.");

            if (surface.ConstructionLayers is null || surface.ConstructionLayers.Count == 0)
                return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' requires at least one construction layer.");

            foreach (var layer in surface.ConstructionLayers)
            {
                if (layer is null)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layers cannot contain null records.");

                if (string.IsNullOrWhiteSpace(layer.LayerId))
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer id is required.");

                if (layer.ThicknessM <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' thickness must be greater than zero.");

                if (layer.ConductivityWPerMK <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' conductivity must be greater than zero.");

                if (layer.DensityKgPerM3 <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' density must be greater than zero.");

                if (layer.SpecificHeatCapacityJPerKgK <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction layer '{layer.LayerId}' specific heat must be greater than zero.");
            }

            foreach (var (name, value) in new[]
            {
                ("boundary conductance", surface.BoundaryConductanceWPerK),
                ("surface-to-air conductance", surface.SurfaceToAirConductanceWPerK),
                ("surface-to-mass conductance", surface.SurfaceToMassConductanceWPerK),
                ("surface heat capacity", surface.HeatCapacityJPerK),
                ("mass heat capacity", surface.MassHeatCapacityJPerK)
            })
            {
                if (value.HasValue && value.Value <= 0)
                    return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' {name} must be greater than zero.");
            }

            nodeIds.Add(ResolveSurfaceNodeId(surface));
            nodeIds.Add(ResolveMassNodeId(surface));
        }

        if (nodeIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != nodeIds.Count)
            return Result.Validation("ISO 52016 physical room model expanded surface node ids must be unique.");

        var solarValidation = ValidateDistributionFractions(
            surfaces,
            static surface => surface.SolarGainsDistributionFraction,
            "solar gains distribution fractions");

        if (solarValidation.IsFailure)
            return solarValidation;

        var radiativeValidation = ValidateDistributionFractions(
            surfaces,
            static surface => surface.InternalRadiativeGainsDistributionFraction,
            "internal radiative gains distribution fractions");

        if (radiativeValidation.IsFailure)
            return radiativeValidation;

        return Result.Success();
    }

    private static Result ValidateSurfaceBoundaryConditions(
        IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> surfaceBoundaryConditions,
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Iso52016RoomHourlyInputProfile hourlyInputProfile)
    {
        if (surfaceBoundaryConditions.Count == 0)
            return Result.Success();

        if (surfaces.Count == 0)
            return Result.Validation("ISO 52016 physical room model surface boundary conditions require explicit surfaces.");

        if (surfaceBoundaryConditions.Any(condition => condition is null))
            return Result.Validation("ISO 52016 physical room model surface boundary conditions cannot contain null records.");

        var surfaceIds = surfaces
            .Select(surface => surface.SurfaceId.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hourIds = hourlyInputProfile.Hours
            .Select(hour => hour.HourOfYear)
            .ToHashSet();

        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var condition in surfaceBoundaryConditions)
        {
            if (string.IsNullOrWhiteSpace(condition.SurfaceId))
                return Result.Validation("ISO 52016 physical room model surface boundary condition surface id is required.");

            var surfaceId = condition.SurfaceId.Trim();

            if (!surfaceIds.Contains(surfaceId))
                return Result.Validation($"ISO 52016 physical room model boundary condition references unknown surface id '{surfaceId}'.");

            if (!hourIds.Contains(condition.HourOfYear))
                return Result.Validation($"ISO 52016 physical room model boundary condition for surface '{surfaceId}' references hour {condition.HourOfYear} that is not in the hourly profile.");

            if (double.IsNaN(condition.BoundaryTemperatureC) || double.IsInfinity(condition.BoundaryTemperatureC))
                return Result.Validation($"ISO 52016 physical room model boundary condition for surface '{surfaceId}' at hour {condition.HourOfYear} must be finite.");

            var key = $"{surfaceId}|{condition.HourOfYear}";

            if (!uniqueKeys.Add(key))
                return Result.Validation($"ISO 52016 physical room model duplicate boundary condition for surface '{surfaceId}' and hour {condition.HourOfYear}.");
        }

        return Result.Success();
    }

    private static Result ValidateOperationConditions(
        IReadOnlyList<Iso52016PhysicalHourlyOperationCondition> operationConditions,
        Iso52016RoomHourlyInputProfile hourlyInputProfile)
    {
        if (operationConditions.Count == 0)
            return Result.Success();

        if (operationConditions.Any(condition => condition is null))
            return Result.Validation("ISO 52016 physical room model operation conditions cannot contain null records.");

        var hourIds = hourlyInputProfile.Hours
            .Select(hour => hour.HourOfYear)
            .ToHashSet();
        var uniqueHours = new HashSet<int>();

        foreach (var condition in operationConditions)
        {
            if (!hourIds.Contains(condition.HourOfYear))
                return Result.Validation($"ISO 52016 physical room model operation condition references hour {condition.HourOfYear} that is not in the hourly profile.");

            if (!uniqueHours.Add(condition.HourOfYear))
                return Result.Validation($"ISO 52016 physical room model duplicate operation condition for hour {condition.HourOfYear}.");

            if (condition.VentilationHeatTransferCoefficientWPerK.HasValue &&
                (double.IsNaN(condition.VentilationHeatTransferCoefficientWPerK.Value) ||
                 double.IsInfinity(condition.VentilationHeatTransferCoefficientWPerK.Value) ||
                 condition.VentilationHeatTransferCoefficientWPerK.Value < 0))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition ventilation heat transfer coefficient must be finite and non-negative at hour {condition.HourOfYear}.");
            }

            if (condition.VentilationBoundaryTemperatureC.HasValue &&
                (double.IsNaN(condition.VentilationBoundaryTemperatureC.Value) ||
                 double.IsInfinity(condition.VentilationBoundaryTemperatureC.Value)))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition ventilation boundary temperature must be finite at hour {condition.HourOfYear}.");
            }

            if (condition.InternalGainsConvectiveFraction.HasValue &&
                (condition.InternalGainsConvectiveFraction.Value < 0 || condition.InternalGainsConvectiveFraction.Value > 1))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition internal gains convective fraction must be between 0 and 1 at hour {condition.HourOfYear}.");
            }

            if (condition.SolarGainsToAirFraction.HasValue &&
                (condition.SolarGainsToAirFraction.Value < 0 || condition.SolarGainsToAirFraction.Value > 1))
            {
                return Result.Validation($"ISO 52016 physical room model operation condition solar gains to air fraction must be between 0 and 1 at hour {condition.HourOfYear}.");
            }
        }

        return Result.Success();
    }

    private static Result ValidateDistributionFractions(
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Func<Iso52016PhysicalSurface, double?> selector,
        string label)
    {
        var configured = surfaces
            .Where(surface => selector(surface).HasValue)
            .ToArray();

        if (configured.Length == 0)
            return Result.Success();

        if (configured.Length != surfaces.Count)
            return Result.Validation($"ISO 52016 physical room model {label} must be specified for every surface when any configured fraction is used.");

        foreach (var surface in configured)
        {
            var value = selector(surface)!.Value;

            if (value < 0 || value > 1)
                return Result.Validation($"ISO 52016 physical room model surface '{surface.SurfaceId}' {label} must be between 0 and 1.");
        }

        var sum = configured.Sum(surface => selector(surface)!.Value);

        if (Math.Abs(sum - 1.0) > FractionTolerance)
            return Result.Validation($"ISO 52016 physical room model {label} must sum to 1.0.");

        return Result.Success();
    }

    private static Dictionary<string, Dictionary<int, double>> BuildBoundaryConditionLookup(
        IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> surfaceBoundaryConditions)
    {
        var lookup = new Dictionary<string, Dictionary<int, double>>(StringComparer.OrdinalIgnoreCase);

        foreach (var condition in surfaceBoundaryConditions)
        {
            var surfaceId = condition.SurfaceId.Trim();

            if (!lookup.TryGetValue(surfaceId, out var hourlyConditions))
            {
                hourlyConditions = new Dictionary<int, double>();
                lookup[surfaceId] = hourlyConditions;
            }

            hourlyConditions[condition.HourOfYear] = condition.BoundaryTemperatureC;
        }

        return lookup;
    }

    private static Dictionary<int, Iso52016PhysicalHourlyOperationCondition> BuildOperationConditionLookup(
        IReadOnlyList<Iso52016PhysicalHourlyOperationCondition> operationConditions) =>
        operationConditions.ToDictionary(
            condition => condition.HourOfYear,
            condition => condition);

    private static Dictionary<int, double> BuildVentilationConductanceByHour(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour)
    {
        var result = new Dictionary<int, double>();

        foreach (var hour in hourlyInputProfile.Hours)
        {
            var conductance = operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
                operationCondition.VentilationHeatTransferCoefficientWPerK.HasValue
                    ? operationCondition.VentilationHeatTransferCoefficientWPerK.Value
                    : hour.VentilationHeatTransferCoefficientWPerK;

            result[hour.HourOfYear] = conductance;
        }

        return result;
    }

    private static double ResolveVentilationBoundaryTemperatureC(
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
        operationCondition.VentilationBoundaryTemperatureC.HasValue
            ? operationCondition.VentilationBoundaryTemperatureC.Value
            : hour.OutdoorTemperatureC;

    private static double ResolveInternalGainsConvectiveFraction(
        Iso52016PhysicalNodeModelOptions modelOptions,
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
        operationCondition.InternalGainsConvectiveFraction.HasValue
            ? operationCondition.InternalGainsConvectiveFraction.Value
            : modelOptions.InternalGainsConvectiveFraction;

    private static double ResolveSolarGainsToAirFraction(
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
        operationCondition.SolarGainsToAirFraction.HasValue
            ? operationCondition.SolarGainsToAirFraction.Value
            : 0.0;

    private static Dictionary<string, double> BuildSurfaceDistribution(
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Func<Iso52016PhysicalSurface, double?> selector)
    {
        var dictionary = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var hasConfiguredFractions = surfaces.Any(surface => selector(surface).HasValue);

        if (hasConfiguredFractions)
        {
            foreach (var surface in surfaces)
            {
                dictionary[surface.SurfaceId.Trim()] = selector(surface)!.Value;
            }

            return dictionary;
        }

        var totalAreaM2 = surfaces.Sum(surface => surface.AreaM2);

        foreach (var surface in surfaces)
        {
            dictionary[surface.SurfaceId.Trim()] = surface.AreaM2 / totalAreaM2;
        }

        return dictionary;
    }

    private static string ResolveSurfaceNodeId(
        Iso52016PhysicalSurface surface) =>
        string.IsNullOrWhiteSpace(surface.SurfaceNodeId)
            ? $"surface:{surface.SurfaceId.Trim()}"
            : surface.SurfaceNodeId.Trim();

    private static string ResolveMassNodeId(
        Iso52016PhysicalSurface surface) =>
        string.IsNullOrWhiteSpace(surface.MassNodeId)
            ? $"mass:{surface.SurfaceId.Trim()}"
            : surface.MassNodeId.Trim();

    private static string ResolveBoundaryId(
        Iso52016PhysicalSurface surface,
        Iso52016PhysicalNodeModelOptions modelOptions,
        ISet<string>? surfacesWithHourlyBoundaryConditions = null)
    {
        if (!string.IsNullOrWhiteSpace(surface.BoundaryId))
            return surface.BoundaryId.Trim();

        var boundaryId = surface.BoundaryType switch
        {
            Iso52016PhysicalSurfaceBoundaryType.Outdoor => modelOptions.OutdoorBoundaryId.Trim(),
            Iso52016PhysicalSurfaceBoundaryType.Ground => modelOptions.GroundBoundaryId.Trim(),
            Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned => modelOptions.AdjacentConditionedBoundaryId.Trim(),
            Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned => modelOptions.AdjacentUnconditionedBoundaryId.Trim(),
            _ => throw new InvalidOperationException("Unsupported ISO 52016 physical surface boundary type.")
        };

        if (surfacesWithHourlyBoundaryConditions?.Contains(surface.SurfaceId.Trim()) == true)
            return $"{boundaryId}:{surface.SurfaceId.Trim()}";

        return boundaryId;
    }

    private static double ResolveBoundaryTemperatureC(
        Iso52016PhysicalSurface surface,
        Iso52016PhysicalNodeModelOptions modelOptions,
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<string, Dictionary<int, double>> boundaryConditionsBySurface)
    {
        var surfaceId = surface.SurfaceId.Trim();

        if (boundaryConditionsBySurface.TryGetValue(surfaceId, out var hourlyConditions) &&
            hourlyConditions.TryGetValue(hour.HourOfYear, out var boundaryTemperatureC))
        {
            return boundaryTemperatureC;
        }

        return surface.BoundaryType switch
        {
            Iso52016PhysicalSurfaceBoundaryType.Outdoor => hour.OutdoorTemperatureC,
            Iso52016PhysicalSurfaceBoundaryType.Ground => hour.GroundBoundaryTemperatureC,
            Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned => surface.AdjacentBoundaryTemperatureC ?? modelOptions.AdjacentBoundaryTemperatureC,
            Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned => surface.AdjacentBoundaryTemperatureC ?? modelOptions.AdjacentBoundaryTemperatureC,
            _ => throw new InvalidOperationException("Unsupported ISO 52016 physical surface boundary type.")
        };
    }

    private static double CalculateBoundaryConductanceWPerK(
        Iso52016PhysicalSurface surface)
    {
        var totalResistanceM2KPerW = surface.ConstructionLayers.Sum(
            layer => layer.ThermalResistanceM2KPerW);

        return surface.AreaM2 / totalResistanceM2KPerW;
    }

    private static double CalculateConstructionHeatCapacityJPerK(
        Iso52016PhysicalSurface surface) =>
        surface.AreaM2 * surface.ConstructionLayers.Sum(
            layer => layer.HeatCapacityJPerM2K);
}