using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal readonly record struct Iso52016HourlyHeatBalanceNormalizedContext(
    IReadOnlyCollection<Room> Rooms,
    int HourOfYear,
    int HourOfDay,
    int DayOfYear,
    Dictionary<int, RoomHourlyProfileSnapshot>? AnnualSnapshots,
    double OccupiedScheduleFactor,
    double HeatingSetpointC,
    double CoolingSetpointC);

internal static class Iso52016HourlyHeatBalanceRequestNormalizer
{
    public static Iso52016HourlyHeatBalanceNormalizedContext Normalize(
        Iso52016ThermalZoneGroup zone,
        Iso52016ThermalZoneState state,
        AnnualHourlyData weather,
        AnnualProfileOptionsDto? annualProfileOptions,
        Iso52016EnergyNeedOptions options,
        HourlyInternalGainProfileService hourlyProfiles,
        Func<Room, int, double> occupancyFactorResolver)
    {
        var rooms = zone.Rooms;
        var hourOfYear = weather.HourOfYear;
        var hourOfDay = hourOfYear % 24;
        var dayOfYear = hourOfYear / 24 + 1;

        Dictionary<int, RoomHourlyProfileSnapshot>? annualSnapshots = null;
        if (annualProfileOptions?.UseAnnualProfiles == true)
        {
            annualSnapshots = rooms.ToDictionary(
                room => room.Id,
                room => hourlyProfiles.GetRoomHourlyMultipliers(
                    room,
                    hourOfYear,
                    annualProfileOptions));
        }

        var occupiedScheduleFactor = Iso52016HourlyCalculatorMath.WeightedAverage(
            rooms,
            room =>
            {
                if (annualSnapshots is not null &&
                    annualSnapshots.TryGetValue(room.Id, out var snapshot))
                {
                    return snapshot.Occupancy;
                }

                return occupancyFactorResolver(room, hourOfDay);
            });

        var heatingSetpoint = occupiedScheduleFactor > 0
            ? state.HeatingSetpointC
            : options.DefaultHeatingSetbackC;

        var coolingSetpoint = occupiedScheduleFactor > 0
            ? state.CoolingSetpointC
            : options.DefaultCoolingSetbackC;

        return new Iso52016HourlyHeatBalanceNormalizedContext(
            rooms,
            hourOfYear,
            hourOfDay,
            dayOfYear,
            annualSnapshots,
            occupiedScheduleFactor,
            heatingSetpoint,
            coolingSetpoint);
    }
}
