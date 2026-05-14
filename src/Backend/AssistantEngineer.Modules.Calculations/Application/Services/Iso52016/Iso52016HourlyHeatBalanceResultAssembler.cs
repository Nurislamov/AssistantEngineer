using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal static class Iso52016HourlyHeatBalanceResultAssembler
{
    public static Iso52016RoomHourResult BuildRoomResult(
        Room room,
        string zoneName,
        int hourOfYear,
        AnnualHourlyData weather,
        double heatingLoad,
        double coolingLoad,
        double operativeTemperature,
        double internalGains,
        double solarGains,
        double transmissionW,
        Iso52016HourlyVentilationComponents ventilationComponentLoads,
        double groundW,
        double transmissionBalanceW,
        double groundBalanceW)
    {
        return new Iso52016RoomHourResult(
            room.Id,
            new Iso52016RoomHourlyEnergyNeed(
                RoomId: room.Id,
                RoomName: room.Name,
                ZoneName: zoneName,
                HourOfYear: hourOfYear,
                Month: Iso52016HourlyCalculatorMath.GetMonth(hourOfYear),
                HeatingLoadW: Iso52016HourlyCalculatorMath.Round(heatingLoad),
                CoolingLoadW: Iso52016HourlyCalculatorMath.Round(coolingLoad),
                OperativeTemperatureC: Iso52016HourlyCalculatorMath.Round(operativeTemperature),
                OutdoorTemperatureC: Iso52016HourlyCalculatorMath.Round(weather.DryBulbTemperature),
                InternalGainsW: Iso52016HourlyCalculatorMath.Round(internalGains),
                SolarGainsW: Iso52016HourlyCalculatorMath.Round(solarGains),
                TransmissionW: Iso52016HourlyCalculatorMath.Round(transmissionW),
                VentilationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.TotalVentilationW),
                InfiltrationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.InfiltrationW),
                GroundW: Iso52016HourlyCalculatorMath.Round(groundW),
                TransmissionBalanceW: Iso52016HourlyCalculatorMath.Round(transmissionBalanceW),
                VentilationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.TotalVentilationBalanceW),
                InfiltrationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.InfiltrationBalanceW),
                GroundBalanceW: Iso52016HourlyCalculatorMath.Round(groundBalanceW),
                MechanicalVentilationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.MechanicalVentilationW),
                NaturalVentilationW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.NaturalVentilationW),
                MechanicalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.MechanicalVentilationBalanceW),
                NaturalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(ventilationComponentLoads.NaturalVentilationBalanceW)));
    }

    public static Iso52016ZoneHourResult BuildZoneResult(
        string zoneName,
        IReadOnlyCollection<Room> rooms,
        IReadOnlyList<Iso52016RoomHourResult> roomResults,
        int hourOfYear,
        AnnualHourlyData weather)
    {
        var zoneHeating = roomResults.Sum(x => x.Hour.HeatingLoadW);
        var zoneCooling = roomResults.Sum(x => x.Hour.CoolingLoadW);
        var zoneInternal = roomResults.Sum(x => x.Hour.InternalGainsW);
        var zoneSolar = roomResults.Sum(x => x.Hour.SolarGainsW);
        var zoneTransmission = roomResults.Sum(x => x.Hour.TransmissionW);
        var zoneVentilation = roomResults.Sum(x => x.Hour.VentilationW);
        var zoneMechanicalVentilation = roomResults.Sum(x => x.Hour.MechanicalVentilationW);
        var zoneNaturalVentilation = roomResults.Sum(x => x.Hour.NaturalVentilationW);
        var zoneInfiltration = roomResults.Sum(x => x.Hour.InfiltrationW);
        var zoneGround = roomResults.Sum(x => x.Hour.GroundW);

        var zoneTransmissionBalance = roomResults.Sum(x => x.Hour.TransmissionBalanceW);
        var zoneVentilationBalance = roomResults.Sum(x => x.Hour.VentilationBalanceW);
        var zoneMechanicalVentilationBalance = roomResults.Sum(x => x.Hour.MechanicalVentilationBalanceW);
        var zoneNaturalVentilationBalance = roomResults.Sum(x => x.Hour.NaturalVentilationBalanceW);
        var zoneInfiltrationBalance = roomResults.Sum(x => x.Hour.InfiltrationBalanceW);
        var zoneGroundBalance = roomResults.Sum(x => x.Hour.GroundBalanceW);

        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        var zoneOperative = totalArea > 0
            ? rooms.Join(
                    roomResults,
                    room => room.Id,
                    result => result.RoomId,
                    (room, result) => room.Area.SquareMeters * result.Hour.OperativeTemperatureC)
                .Sum() / totalArea
            : roomResults.Average(x => x.Hour.OperativeTemperatureC);

        return new Iso52016ZoneHourResult(
            zoneName,
            new Iso52016HourlyEnergyNeed(
                HourOfYear: hourOfYear,
                Month: Iso52016HourlyCalculatorMath.GetMonth(hourOfYear),
                HeatingLoadW: Iso52016HourlyCalculatorMath.Round(zoneHeating),
                CoolingLoadW: Iso52016HourlyCalculatorMath.Round(zoneCooling),
                OperativeTemperatureC: Iso52016HourlyCalculatorMath.Round(zoneOperative),
                OutdoorTemperatureC: Iso52016HourlyCalculatorMath.Round(weather.DryBulbTemperature),
                InternalGainsW: Iso52016HourlyCalculatorMath.Round(zoneInternal),
                SolarGainsW: Iso52016HourlyCalculatorMath.Round(zoneSolar),
                TransmissionW: Iso52016HourlyCalculatorMath.Round(zoneTransmission),
                VentilationW: Iso52016HourlyCalculatorMath.Round(zoneVentilation),
                InfiltrationW: Iso52016HourlyCalculatorMath.Round(zoneInfiltration),
                GroundW: Iso52016HourlyCalculatorMath.Round(zoneGround),
                TransmissionBalanceW: Iso52016HourlyCalculatorMath.Round(zoneTransmissionBalance),
                VentilationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneVentilationBalance),
                InfiltrationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneInfiltrationBalance),
                GroundBalanceW: Iso52016HourlyCalculatorMath.Round(zoneGroundBalance),
                MechanicalVentilationW: Iso52016HourlyCalculatorMath.Round(zoneMechanicalVentilation),
                NaturalVentilationW: Iso52016HourlyCalculatorMath.Round(zoneNaturalVentilation),
                MechanicalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneMechanicalVentilationBalance),
                NaturalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(zoneNaturalVentilationBalance)),
            roomResults);
    }
}
