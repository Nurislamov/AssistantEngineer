using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomHourlyInputProfileBuilder : ISo52016RoomHourlyInputProfileBuilder
{
    public Result<Iso52016RoomHourlyInputProfile> Build(
        Iso52016RoomHourlyInputProfileRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomHourlyInputProfile>.Failure(validation);

        var hours = Enumerable
            .Range(0, request.WeatherSolarContext.HourCount)
            .Select(hourOfYear => BuildHour(
                request,
                hourOfYear))
            .ToArray();

        return Result<Iso52016RoomHourlyInputProfile>.Success(
            new Iso52016RoomHourlyInputProfile(
                RoomCode: request.RoomCode.Trim(),
                TransmissionHeatTransferCoefficientWPerK: request.TransmissionHeatTransferCoefficientWPerK,
                VentilationHeatTransferCoefficientWPerK: request.VentilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: request.ThermalCapacityJPerK,
                HeatingSetpointC: request.HeatingSetpointC,
                CoolingSetpointC: request.CoolingSetpointC,
                Hours: hours));
    }

    private static Iso52016RoomHourlyInputRecord BuildHour(
        Iso52016RoomHourlyInputProfileRequest request,
        int hourOfYear)
    {
        var weather = request.WeatherSolarContext.GetHour(
            hourOfYear);

        var solar = request.SolarGainProfile.GetHour(
            hourOfYear);

        var internalGains = request.InternalGainProfile.GetHour(
            hourOfYear);

        var totalHeatTransferCoefficient =
            request.TransmissionHeatTransferCoefficientWPerK +
            request.VentilationHeatTransferCoefficientWPerK;

        var totalGains =
            solar.TotalSolarGainW +
            internalGains.TotalInternalGainW;

        return new Iso52016RoomHourlyInputRecord(
            HourOfYear: hourOfYear,
            Month: weather.Month,
            Day: weather.Day,
            Hour: weather.Hour,
            OutdoorTemperatureC: weather.OutdoorTemperatureC,
            GroundBoundaryTemperatureC: weather.GroundBoundaryTemperatureC,
            HeatingSetpointC: request.HeatingSetpointC,
            CoolingSetpointC: request.CoolingSetpointC,
            TransmissionHeatTransferCoefficientWPerK: request.TransmissionHeatTransferCoefficientWPerK,
            VentilationHeatTransferCoefficientWPerK: request.VentilationHeatTransferCoefficientWPerK,
            TotalHeatTransferCoefficientWPerK: totalHeatTransferCoefficient,
            ThermalCapacityJPerK: request.ThermalCapacityJPerK,
            SolarGainsW: solar.TotalSolarGainW,
            InternalGainsW: internalGains.TotalInternalGainW,
            TotalGainsW: totalGains);
    }

    private static Result Validate(
        Iso52016RoomHourlyInputProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomCode))
            return Result.Validation("Room code is required.");

        if (request.WeatherSolarContext is null)
            return Result.Validation("ISO 52016 weather-solar context is required.");

        if (request.SolarGainProfile is null)
            return Result.Validation("Room solar gain profile is required.");

        if (request.InternalGainProfile is null)
            return Result.Validation("Room internal gain profile is required.");

        if (request.WeatherSolarContext.HourCount == 0)
            return Result.Validation("ISO 52016 weather-solar context must contain hourly records.");

        if (request.SolarGainProfile.HourCount != request.WeatherSolarContext.HourCount)
        {
            return Result.Validation(
                "Room solar gain profile hour count must match ISO 52016 weather-solar context hour count.");
        }

        if (request.InternalGainProfile.HourCount != request.WeatherSolarContext.HourCount)
        {
            return Result.Validation(
                "Room internal gain profile hour count must match ISO 52016 weather-solar context hour count.");
        }

        if (!RoomCodeMatches(request.RoomCode, request.SolarGainProfile.RoomCode))
        {
            return Result.Validation(
                "Room solar gain profile room code must match request room code.");
        }

        if (!RoomCodeMatches(request.RoomCode, request.InternalGainProfile.RoomCode))
        {
            return Result.Validation(
                "Room internal gain profile room code must match request room code.");
        }

        if (request.TransmissionHeatTransferCoefficientWPerK < 0)
        {
            return Result.Validation(
                "Transmission heat transfer coefficient must not be negative.");
        }

        if (request.VentilationHeatTransferCoefficientWPerK < 0)
        {
            return Result.Validation(
                "Ventilation heat transfer coefficient must not be negative.");
        }

        if (request.TransmissionHeatTransferCoefficientWPerK == 0 &&
            request.VentilationHeatTransferCoefficientWPerK == 0)
        {
            return Result.Validation(
                "At least one heat transfer coefficient must be greater than zero.");
        }

        if (request.ThermalCapacityJPerK <= 0)
            return Result.Validation("Thermal capacity must be greater than zero.");

        if (request.CoolingSetpointC <= request.HeatingSetpointC)
        {
            return Result.Validation(
                "Cooling setpoint must be greater than heating setpoint.");
        }

        return Result.Success();
    }

    private static bool RoomCodeMatches(
        string expected,
        string actual) =>
        string.Equals(
            expected.Trim(),
            actual.Trim(),
            StringComparison.OrdinalIgnoreCase);
}