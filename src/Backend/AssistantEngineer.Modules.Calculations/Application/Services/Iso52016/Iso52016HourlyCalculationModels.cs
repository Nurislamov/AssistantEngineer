using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal sealed record Iso52016HourlyWeatherContext(
    int Year,
    AnnualHourlyData[] HourlyData,
    double[] GroundBoundaryTemperaturesC,
    Iso52016WeatherSolarContext? WeatherSolarContext = null);

internal sealed record Iso52016ThermalZoneGroup(
    string Name,
    IReadOnlyCollection<Room> Rooms);

internal sealed record Iso52016ThermalZoneState(
    double FloorAreaM2,
    double VolumeM3,
    double OutdoorBoundaryHeatTransferCoefficientWPerK,
    double GroundBoundaryHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double HeatingSetpointC,
    double CoolingSetpointC);

internal sealed record Iso52016AdjacentBoundaryContribution(
    double HeatTransferCoefficientWPerK,
    double BoundaryTemperatureWeightedHeatTransferW);

internal sealed record Iso52016HourlyVentilationComponents(
    double MechanicalHeatTransferWPerK,
    double NaturalHeatTransferWPerK,
    double InfiltrationHeatTransferWPerK,
    double TotalVentilationHeatTransferWPerK,
    double MechanicalVentilationW = 0,
    double NaturalVentilationW = 0,
    double InfiltrationW = 0,
    double TotalVentilationW = 0,
    double MechanicalVentilationBalanceW = 0,
    double NaturalVentilationBalanceW = 0,
    double InfiltrationBalanceW = 0,
    double TotalVentilationBalanceW = 0)
{
    public Iso52016HourlyVentilationComponents WithLoads(
        double outdoorTemperatureC,
        double operativeTemperatureC)
    {
        var deltaT = outdoorTemperatureC - operativeTemperatureC;
        var magnitudeDeltaT = Math.Abs(deltaT);
        var mechanicalW = MechanicalHeatTransferWPerK * magnitudeDeltaT;
        var naturalW = NaturalHeatTransferWPerK * magnitudeDeltaT;
        var infiltrationW = InfiltrationHeatTransferWPerK * magnitudeDeltaT;
        var mechanicalBalanceW = MechanicalHeatTransferWPerK * deltaT;
        var naturalBalanceW = NaturalHeatTransferWPerK * deltaT;
        var infiltrationBalanceW = InfiltrationHeatTransferWPerK * deltaT;

        return this with
        {
            MechanicalVentilationW = mechanicalW,
            NaturalVentilationW = naturalW,
            InfiltrationW = infiltrationW,
            TotalVentilationW = mechanicalW + naturalW,
            MechanicalVentilationBalanceW = mechanicalBalanceW,
            NaturalVentilationBalanceW = naturalBalanceW,
            InfiltrationBalanceW = infiltrationBalanceW,
            TotalVentilationBalanceW = mechanicalBalanceW + naturalBalanceW
        };
    }
}

internal sealed record Iso52016RoomHourResult(
    int RoomId,
    Iso52016RoomHourlyEnergyNeed Hour);

internal sealed record Iso52016ZoneHourResult(
    string ZoneName,
    Iso52016HourlyEnergyNeed Hour,
    IReadOnlyCollection<Iso52016RoomHourResult> Rooms);

internal sealed record Iso52016HourlyBuildingCalculationContext(
    IReadOnlyList<Iso52016ThermalZoneGroup> Zones,
    IReadOnlyDictionary<string, Iso52016ThermalZoneState> ZoneStates,
    IReadOnlyDictionary<int, string> RoomZoneMap,
    Dictionary<int, double> PreviousRoomTemperatures);

internal sealed record Iso52016HourlyZoneCalculationContext(
    Iso52016ThermalZoneGroup Zone,
    Iso52016ThermalZoneState ZoneState,
    IReadOnlyDictionary<int, string> RoomZoneMap,
    Dictionary<int, double> PreviousRoomTemperatures);

