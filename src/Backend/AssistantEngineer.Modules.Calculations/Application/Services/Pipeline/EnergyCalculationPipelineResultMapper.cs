using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal static class EnergyCalculationPipelineResultMapper
{
    public static void AddMethodCompatibilityDiagnostic(
        List<CalculationDiagnostic> diagnostics,
        string? requestedMethod,
        string context,
        string actualMethodLabel)
    {
        if (string.IsNullOrWhiteSpace(requestedMethod))
            return;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "CalculationMethod.ApiCompatibility",
            $"Requested method '{requestedMethod}' is accepted for API compatibility, but this endpoint currently uses {actualMethodLabel}.",
            context));
    }

    public static EffectiveVentilationAssumption ResolveEffectiveVentilationAssumption(
        Room room,
        CalculationPreferences preferences,
        double deltaT)
    {
        var ventilation = room.VentilationParameters;
        var volumeM3 = room.CalculateVolume();
        var mechanicalAch = ventilation?.AirChangesPerHour ??
            preferences.Iso52016DefaultAirChangesPerHour;
        var infiltrationAch = ventilation is null
            ? 0
            : ventilation.InfiltrationAirChangesPerHour + ventilation.StackCoefficient * Math.Sqrt(deltaT);
        var source = ventilation is null
            ? "DefaultCalculationPreferences"
            : "RoomVentilationParameters";

        return new EffectiveVentilationAssumption(
            EffectiveAirChangesPerHour: mechanicalAch,
            EffectiveMechanicalAirflowM3PerHour: mechanicalAch * volumeM3,
            EffectiveInfiltrationAirChangesPerHour: infiltrationAch,
            EffectiveInfiltrationAirflowM3PerHour: infiltrationAch * volumeM3,
            Source: source);
    }

    public static RoomCalculationResult MapCoolingRoomResult(
        Room room,
        RoomLoadCalculationResult load,
        CalculationPreferences preferences,
        CoolingLoadCalculationMethod requestedMethod,
        string roomPipelineMethod,
        string energyCalculationParityDesignPoint)
    {
        var reserveFactor = preferences.CoolingSafetyFactor;
        var designCapacity = load.CoolingLoadW * reserveFactor;
        var internalGain = load.CoolingBreakdown.InternalGainsW;
        var outdoorTemperature = room.OutdoorTemperatureOverride?.Celsius ??
            room.Floor.Building.ClimateZone?.SummerDesignTemperature.Celsius ??
            room.IndoorTemperature.Celsius;
        var ventilationAssumption = ResolveEffectiveVentilationAssumption(
            room,
            preferences,
            Math.Max(outdoorTemperature - room.IndoorTemperature.Celsius, 0));

        return new RoomCalculationResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = roomPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = energyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation equivalence design-point pipeline",
            PeakHourOfYear = null,
            AreaM2 = Round(room.Area.SquareMeters),
            HeightM = Round(room.HeightM),
            VolumeM3 = Round(room.CalculateVolume()),
            IndoorTemperatureC = Round(room.IndoorTemperature.Celsius),
            OutdoorTemperatureC = Round(outdoorTemperature),
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = Round(room.EquipmentLoad.Watts),
            LightingLoadW = Round(room.LightingLoad.Watts),
            EffectiveAirChangesPerHour = Round(ventilationAssumption.EffectiveAirChangesPerHour),
            EffectiveMechanicalAirflowM3PerHour = Round(ventilationAssumption.EffectiveMechanicalAirflowM3PerHour),
            EffectiveInfiltrationAirChangesPerHour = Round(ventilationAssumption.EffectiveInfiltrationAirChangesPerHour),
            EffectiveInfiltrationAirflowM3PerHour = Round(ventilationAssumption.EffectiveInfiltrationAirflowM3PerHour),
            VentilationAssumptionSource = ventilationAssumption.Source,
            TotalWindowAreaM2 = Round(room.Windows.Sum(window => window.Area.SquareMeters)),
            TotalWallAreaM2 = Round(room.Walls.Sum(wall => wall.Area.SquareMeters)),
            ExternalWallAreaM2 = Round(room.Walls.Where(wall => wall.IsExternal).Sum(wall => wall.Area.SquareMeters)),
            WindowHeatGainW = Round(load.CoolingBreakdown.WindowTransmissionW + load.CoolingBreakdown.SolarW),
            WallHeatGainW = Round(load.CoolingBreakdown.TransmissionW + load.CoolingBreakdown.GroundW),
            VentilationHeatGainW = Round(load.CoolingBreakdown.VentilationW),
            InfiltrationHeatGainW = Round(load.CoolingBreakdown.InfiltrationW),
            PeopleHeatGainW = 0,
            EquipmentHeatGainW = Round(room.EquipmentLoad.Watts),
            LightingHeatGainW = Round(room.LightingLoad.Watts),
            InternalHeatGainW = Round(internalGain),
            CoolingLoadW = Round(load.CoolingLoadW),
            CoolingLoadKw = Round(load.CoolingLoadW / 1000.0),
            CoolingLoadWPerM2 = Round(load.CoolingLoadWPerM2),
            DeltaTemperatureC = Round(Math.Abs(outdoorTemperature - room.IndoorTemperature.Celsius)),
            HeightAdjustmentFactor = Round(room.HeightM / 3.0),
            TemperatureAdjustmentFactor = 1.0,
            DesignReserveFactor = Round(reserveFactor),
            DesignCapacityW = Round(designCapacity),
            DesignCapacityKw = Round(designCapacity / 1000.0),
            HourlyHeatLoadW = Enumerable.Repeat(Round(load.CoolingLoadW), 24).ToList(),
            Breakdown = load.CoolingBreakdown,
            Diagnostics = load.Diagnostics.ToList(),
            Assumptions = load.AssumptionsUsed.ToList()
        };
    }

    public static RoomHeatingLoadResult MapHeatingRoomResult(
        Room room,
        RoomLoadCalculationResult load,
        HeatingLoadCalculationMethod requestedMethod,
        CalculationPreferences preferences,
        string roomPipelineMethod,
        string energyCalculationParityDesignPoint)
    {
        var ventilation = load.HeatingBreakdown.VentilationW;
        var infiltration = load.HeatingBreakdown.InfiltrationW;
        var transmission = load.HeatingBreakdown.TransmissionW +
            load.HeatingBreakdown.WindowTransmissionW +
            load.HeatingBreakdown.GroundW;
        var outdoorDesignTemperature = room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ??
            room.OutdoorTemperatureOverride?.Celsius ??
            0;
        var ventilationAssumption = ResolveEffectiveVentilationAssumption(
            room,
            preferences,
            Math.Max(room.IndoorTemperature.Celsius - outdoorDesignTemperature, 0));

        return new RoomHeatingLoadResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = roomPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = energyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation equivalence design-point pipeline",
            IndoorDesignTemperatureC = Round(room.IndoorTemperature.Celsius),
            OutdoorDesignTemperatureC = Round(outdoorDesignTemperature),
            DeltaTemperatureC = Round(Math.Max(room.IndoorTemperature.Celsius - outdoorDesignTemperature, 0)),
            VolumeM3 = Round(room.CalculateVolume()),
            AirChangesPerHour = Round(ventilationAssumption.EffectiveAirChangesPerHour),
            EffectiveAirChangesPerHour = Round(ventilationAssumption.EffectiveAirChangesPerHour),
            EffectiveMechanicalAirflowM3PerHour = Round(ventilationAssumption.EffectiveMechanicalAirflowM3PerHour),
            EffectiveInfiltrationAirChangesPerHour = Round(ventilationAssumption.EffectiveInfiltrationAirChangesPerHour),
            EffectiveInfiltrationAirflowM3PerHour = Round(ventilationAssumption.EffectiveInfiltrationAirflowM3PerHour),
            VentilationAssumptionSource = ventilationAssumption.Source,
            TransmissionHeatLossW = Round(transmission),
            VentilationHeatLossW = Round(ventilation + infiltration),
            MechanicalVentilationHeatLossW = Round(ventilation),
            InfiltrationHeatLossW = Round(infiltration),
            NaturalVentilationHeatLossW = 0,
            TotalDesignHeatingLoadW = Round(load.HeatingLoadW),
            TotalDesignHeatingLoadKw = Round(load.HeatingLoadW / 1000.0),
            HeatingLoadW = Round(load.HeatingLoadW),
            HeatingLoadWPerM2 = Round(load.HeatingLoadWPerM2),
            Breakdown = load.HeatingBreakdown,
            Diagnostics = load.Diagnostics.ToList(),
            Assumptions = load.AssumptionsUsed.ToList()
        };
    }

    public static FloorCalculationResult MapFloorResult(
        Floor floor,
        LoadAggregationResult aggregation,
        CalculationPreferences preferences,
        string? requestedMethod,
        string aggregationPipelineMethod,
        string energyCalculationParityDesignPoint)
    {
        var designCapacity = aggregation.CoolingLoadW * preferences.CoolingSafetyFactor;
        var diagnostics = aggregation.Diagnostics.ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod,
            $"Floor {floor.Id} application aggregation",
            "Energy Calculation equivalence design-point aggregation");

        return new FloorCalculationResult
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            CalculationMethod = aggregationPipelineMethod,
            RequestedMethod = requestedMethod ?? string.Empty,
            ActualMethod = energyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation equivalence design-point aggregation",
            PeakHourOfYear = null,
            RoomsCount = aggregation.RoomCount,
            CoolingLoadW = Round(aggregation.CoolingLoadW),
            CoolingLoadKw = Round(aggregation.CoolingLoadW / 1000.0),
            CoolingLoadWPerM2 = Round(aggregation.CoolingLoadWPerM2),
            HeatingLoadW = Round(aggregation.HeatingLoadW),
            HeatingLoadWPerM2 = Round(aggregation.HeatingLoadWPerM2),
            DesignReserveFactor = Round(preferences.CoolingSafetyFactor),
            DesignCapacityW = Round(designCapacity),
            DesignCapacityKw = Round(designCapacity / 1000.0),
            HourlyHeatLoadW = Enumerable.Repeat(Round(aggregation.CoolingLoadW), 24).ToList(),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = diagnostics
        };
    }

    public static BuildingCalculationResult MapBuildingCoolingResult(
        Building building,
        LoadAggregationResult aggregation,
        CalculationPreferences preferences,
        CoolingLoadCalculationMethod requestedMethod,
        string aggregationPipelineMethod,
        string energyCalculationParityDesignPoint)
    {
        var designCapacity = aggregation.CoolingLoadW * preferences.CoolingSafetyFactor;
        var diagnostics = aggregation.Diagnostics.ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod.ToString(),
            $"Building {building.Id} application cooling aggregation",
            "Energy Calculation equivalence design-point aggregation");

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CalculationMethod = aggregationPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = energyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation equivalence design-point aggregation",
            PeakHourOfYear = null,
            FloorsCount = building.Floors.Count,
            RoomsCount = aggregation.RoomCount,
            CoolingLoadW = Round(aggregation.CoolingLoadW),
            CoolingLoadKw = Round(aggregation.CoolingLoadW / 1000.0),
            CoolingLoadWPerM2 = Round(aggregation.CoolingLoadWPerM2),
            DesignReserveFactor = Round(preferences.CoolingSafetyFactor),
            DesignCapacityW = Round(designCapacity),
            DesignCapacityKw = Round(designCapacity / 1000.0),
            HourlyHeatLoadW = Enumerable.Repeat(Round(aggregation.CoolingLoadW), 24).ToList(),
            ThermalZones = BuildThermalZoneResults(building, aggregation),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = diagnostics
        };
    }

    public static BuildingHeatingLoadResult MapBuildingHeatingResult(
        Building building,
        LoadAggregationResult aggregation,
        IReadOnlyList<RoomHeatingLoadResult> rooms,
        HeatingLoadCalculationMethod requestedMethod,
        string aggregationPipelineMethod,
        string energyCalculationParityDesignPoint)
    {
        var transmission = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilation = rooms.Sum(room => room.VentilationHeatLossW);
        var diagnostics = aggregation.Diagnostics.ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            requestedMethod.ToString(),
            $"Building {building.Id} application heating aggregation",
            "Energy Calculation equivalence design-point aggregation");

        return new BuildingHeatingLoadResult
        {
            BuildingId = building.Id,
            ProjectName = building.Project?.Name ?? string.Empty,
            BuildingName = building.Name,
            CalculationMethod = aggregationPipelineMethod,
            RequestedMethod = requestedMethod.ToString(),
            ActualMethod = energyCalculationParityDesignPoint,
            CalculationMethodLabel = "Energy Calculation equivalence design-point aggregation",
            RoomsCount = aggregation.RoomCount,
            TransmissionHeatLossW = Round(transmission),
            VentilationHeatLossW = Round(ventilation),
            TotalDesignHeatingLoadW = Round(aggregation.HeatingLoadW),
            TotalDesignHeatingLoadKw = Round(aggregation.HeatingLoadW / 1000.0),
            HeatingLoadW = Round(aggregation.HeatingLoadW),
            HeatingLoadWPerM2 = Round(aggregation.HeatingLoadWPerM2),
            Rooms = rooms.ToList(),
            ComponentBreakdown = aggregation.ComponentBreakdown,
            Diagnostics = diagnostics
        };
    }

    public static BuildingEnergyBalanceResult MapEnergyBalanceResult(
        BuildingEnergyBalanceResult source,
        AnnualEnergyBalanceResult annual,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        string sourceName,
        bool isTrueHourly8760,
        int hourlyRecordCount,
        IReadOnlyList<CalculationDiagnostic> adapterDiagnostics,
        string energyCalculationParityAnnualAggregationAdapter)
    {
        var diagnostics = source.Diagnostics
            .Concat(annual.Diagnostics)
            .Concat(adapterDiagnostics)
            .ToList();
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            coolingMethod.ToString(),
            $"Building {annual.BuildingId} application annual energy balance cooling method",
            "Energy Calculation equivalence annual aggregation adapter");
        AddMethodCompatibilityDiagnostic(
            diagnostics,
            heatingMethod.ToString(),
            $"Building {annual.BuildingId} application annual energy balance heating method",
            "Energy Calculation equivalence annual aggregation adapter");

        return new BuildingEnergyBalanceResult
        {
            BuildingId = annual.BuildingId,
            BuildingName = annual.BuildingName ?? source.BuildingName,
            CoolingCalculationMethod = "Energy Calculation equivalence / Annual Aggregation Adapter",
            HeatingCalculationMethod = "Energy Calculation equivalence / Annual Aggregation Adapter",
            RequestedCoolingMethod = coolingMethod.ToString(),
            RequestedHeatingMethod = heatingMethod.ToString(),
            ActualMethod = energyCalculationParityAnnualAggregationAdapter,
            CalculationMethodLabel = "Energy Calculation equivalence annual aggregation adapter",
            EnergyDataSource = sourceName,
            IsTrueHourly8760 = isTrueHourly8760,
            HourlyRecordCount = hourlyRecordCount,
            AnnualCoolingDemandKWh = Round(annual.AnnualCoolingDemandKWh),
            AnnualHeatingDemandKWh = Round(annual.AnnualHeatingDemandKWh),
            AnnualTotalDemandKWh = Round(annual.AnnualTotalDemandKWh),
            EnergyUseIntensityKWhPerM2Year = Round(annual.EnergyUseIntensityKWhPerM2Year),
            PeakHeatingW = Round(annual.PeakHeatingLoadW),
            PeakCoolingW = Round(annual.PeakCoolingLoadW),
            PeakHeatingHour = annual.PeakHeatingHour,
            PeakCoolingHour = annual.PeakCoolingHour,
            ComponentBreakdown = annual.ComponentBreakdown,
            MonthlyBalances = annual.MonthlyResults
                .Select(month => new MonthlyEnergyBalance
                {
                    Month = month.Month,
                    HeatingDemandKWh = Round(month.HeatingKWh),
                    CoolingDemandKWh = Round(month.CoolingKWh)
                })
                .ToList(),
            Diagnostics = diagnostics,
            Assumptions = annual.AssumptionsUsed.ToList()
        };
    }

    private static List<ThermalZoneCalculationResult> BuildThermalZoneResults(
        Building building,
        LoadAggregationResult aggregation)
    {
        if (building.ThermalZones.Count == 0)
            return [];

        var roomLoads = aggregation.RoomBreakdown.ToDictionary(room => room.RoomId);
        var countedRooms = new HashSet<int>();
        var results = new List<ThermalZoneCalculationResult>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var rooms = zone.AssignedRooms
                .Where(room => countedRooms.Add(room.Id) && roomLoads.ContainsKey(room.Id))
                .ToArray();
            if (rooms.Length == 0)
                continue;

            var coolingLoad = rooms.Sum(room => roomLoads[room.Id].CoolingLoadW);
            results.Add(new ThermalZoneCalculationResult
            {
                ThermalZoneId = zone.Id,
                ThermalZoneName = zone.Name,
                RoomsCount = rooms.Length,
                PeakHourOfYear = null,
                CoolingLoadW = Round(coolingLoad),
                CoolingLoadKw = Round(coolingLoad / 1000.0),
                RoomIds = rooms.Select(room => room.Id).ToList(),
                HourlyHeatLoadW = Enumerable.Repeat(Round(coolingLoad), 24).ToList()
            });
        }

        var unassigned = building.Floors
            .SelectMany(floor => floor.Rooms)
            .Where(room => !countedRooms.Contains(room.Id) && roomLoads.ContainsKey(room.Id))
            .ToArray();
        if (unassigned.Length > 0)
        {
            var coolingLoad = unassigned.Sum(room => roomLoads[room.Id].CoolingLoadW);
            results.Add(new ThermalZoneCalculationResult
            {
                ThermalZoneName = "Unassigned rooms",
                IsUnassignedRoomsZone = true,
                RoomsCount = unassigned.Length,
                PeakHourOfYear = null,
                CoolingLoadW = Round(coolingLoad),
                CoolingLoadKw = Round(coolingLoad / 1000.0),
                RoomIds = unassigned.Select(room => room.Id).ToList(),
                HourlyHeatLoadW = Enumerable.Repeat(Round(coolingLoad), 24).ToList()
            });
        }

        return results;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
