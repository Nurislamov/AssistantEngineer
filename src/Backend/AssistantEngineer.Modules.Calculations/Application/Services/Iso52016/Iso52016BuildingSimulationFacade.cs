using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016BuildingSimulationFacade : IIso52016BuildingSimulationFacade
{
    private readonly IIso52016WeatherSolarContextBuilder _weatherSolarContextBuilder;
    private readonly IIso52016RoomEnergySimulationRequestBuilder _roomSimulationRequestBuilder;
    private readonly IIso52016RoomEnergySimulationService _roomSimulationService;
    private readonly IIso52016V2RoomEnergySimulationService? _v2RoomSimulationService;
    private readonly IIso52016V2RoomEnergySimulationResultMapper? _v2ResultMapper;

    public Iso52016BuildingSimulationFacade(
        IIso52016WeatherSolarContextBuilder weatherSolarContextBuilder,
        IIso52016RoomEnergySimulationRequestBuilder roomSimulationRequestBuilder,
        IIso52016RoomEnergySimulationService roomSimulationService,
        IIso52016V2RoomEnergySimulationService? v2RoomSimulationService = null,
        IIso52016V2RoomEnergySimulationResultMapper? v2ResultMapper = null)
    {
        _weatherSolarContextBuilder = weatherSolarContextBuilder;
        _roomSimulationRequestBuilder = roomSimulationRequestBuilder;
        _roomSimulationService = roomSimulationService;
        _v2RoomSimulationService = v2RoomSimulationService;
        _v2ResultMapper = v2ResultMapper;
    }

    public Result<Iso52016BuildingSimulationFacadeResult> Simulate(
        Iso52016BuildingSimulationFacadeRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016BuildingSimulationFacadeResult>.Failure(validation);

        var weatherSolarContextResult = _weatherSolarContextBuilder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: request.AnnualClimateData,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                TimeZoneOffset: request.TimeZoneOffset,
                Surfaces: request.Surfaces,
                GroundReflectance: request.GroundReflectance,
                GroundBoundaryTemperature: request.GroundBoundaryTemperature));

        if (weatherSolarContextResult.IsFailure)
            return Result<Iso52016BuildingSimulationFacadeResult>.Failure(weatherSolarContextResult);

        var roomResults = new List<Iso52016RoomEnergySimulationResult>(
            request.Rooms.Count);

        foreach (var room in request.Rooms)
        {
            var roomRequestResult = _roomSimulationRequestBuilder.Build(
                new Iso52016RoomEnergySimulationBuildRequest(
                    Room: room,
                    WeatherSolarContext: weatherSolarContextResult.Value,
                    Defaults: request.Defaults,
                    HeatingSetpointOverrideC: request.HeatingSetpointOverrideC,
                    CoolingSetpointOverrideC: request.CoolingSetpointOverrideC));

            if (roomRequestResult.IsFailure)
                return Result<Iso52016BuildingSimulationFacadeResult>.Failure(roomRequestResult);

            var roomSimulationRequest = roomRequestResult.Value with
            {
                HeatBalanceOptions = request.HeatBalanceOptions
            };

            var roomSimulationResult = request.SimulationEngine switch
            {
                Iso52016SimulationEngine.Legacy => _roomSimulationService.Simulate(
                    roomSimulationRequest),
                Iso52016SimulationEngine.V2Matrix => SimulateRoomWithV2Matrix(
                    roomSimulationRequest),
                _ => Result<Iso52016RoomEnergySimulationResult>.Validation(
                    "Unsupported ISO 52016 simulation engine.")
            };

            if (roomSimulationResult.IsFailure)
                return Result<Iso52016BuildingSimulationFacadeResult>.Failure(roomSimulationResult);

            roomResults.Add(
                roomSimulationResult.Value);
        }

        var hourlyResults = AggregateHourlyResults(
            weatherSolarContextResult.Value,
            roomResults);

        var monthlySummaries = BuildMonthlySummaries(
            hourlyResults);

        return Result<Iso52016BuildingSimulationFacadeResult>.Success(
            new Iso52016BuildingSimulationFacadeResult(
                BuildingCode: request.BuildingCode.Trim(),
                WeatherSolarContext: weatherSolarContextResult.Value,
                RoomResults: roomResults,
                Hours: hourlyResults,
                MonthlySummaries: monthlySummaries,
                SimulationEngine: request.SimulationEngine));
    }

    private Result<Iso52016RoomEnergySimulationResult> SimulateRoomWithV2Matrix(
        Iso52016RoomEnergySimulationRequest roomSimulationRequest)
    {
        if (_v2RoomSimulationService is null)
        {
            return Result<Iso52016RoomEnergySimulationResult>.Failure(
                "ISO 52016 V2 room simulation service is not registered.");
        }

        if (_v2ResultMapper is null)
        {
            return Result<Iso52016RoomEnergySimulationResult>.Failure(
                "ISO 52016 V2 result mapper is not registered.");
        }

        var v2Result = _v2RoomSimulationService.Simulate(
            roomSimulationRequest);

        if (v2Result.IsFailure)
            return Result<Iso52016RoomEnergySimulationResult>.Failure(v2Result);

        return _v2ResultMapper.Map(
            v2Result.Value);
    }
    private static IReadOnlyList<Iso52016HourlyBuildingSimulationRecord> AggregateHourlyResults(
        Iso52016WeatherSolarContext weatherSolarContext,
        IReadOnlyList<Iso52016RoomEnergySimulationResult> roomResults)
    {
        var hours = new List<Iso52016HourlyBuildingSimulationRecord>(
            weatherSolarContext.HourCount);

        for (var hourOfYear = 0; hourOfYear < weatherSolarContext.HourCount; hourOfYear++)
        {
            var weather = weatherSolarContext.GetHour(
                hourOfYear);

            var roomHours = roomResults
                .Select(room =>
                    room.HeatBalanceProfile.GetHour(hourOfYear))
                .ToArray();

            var roomCount = roomHours.Length;

            hours.Add(
                new Iso52016HourlyBuildingSimulationRecord(
                    HourOfYear: hourOfYear,
                    Month: weather.Month,
                    Day: weather.Day,
                    Hour: weather.Hour,
                    OutdoorTemperatureC: weather.OutdoorTemperatureC,
                    AverageIndoorTemperatureC: roomCount == 0
                        ? 0.0
                        : roomHours.Average(hour => hour.IndoorTemperatureAfterHvacC),
                    SolarGainsW: roomHours.Sum(hour => hour.SolarGainsW),
                    InternalGainsW: roomHours.Sum(hour => hour.InternalGainsW),
                    TotalGainsW: roomHours.Sum(hour => hour.TotalGainsW),
                    HeatingLoadW: roomHours.Sum(hour => hour.HeatingLoadW),
                    CoolingLoadW: roomHours.Sum(hour => hour.CoolingLoadW),
                    HeatingEnergyKWh: roomHours.Sum(hour => hour.HeatingEnergyKWh),
                    CoolingEnergyKWh: roomHours.Sum(hour => hour.CoolingEnergyKWh)));
        }

        return hours;
    }

    private static IReadOnlyList<Iso52016MonthlyBuildingSimulationSummary> BuildMonthlySummaries(
        IReadOnlyList<Iso52016HourlyBuildingSimulationRecord> hours) =>
        hours
            .GroupBy(hour => hour.Month)
            .OrderBy(group => group.Key)
            .Select(group => new Iso52016MonthlyBuildingSimulationSummary(
                Month: group.Key,
                HeatingEnergyKWh: group.Sum(hour => hour.HeatingEnergyKWh),
                CoolingEnergyKWh: group.Sum(hour => hour.CoolingEnergyKWh),
                SolarGainsKWh: group.Sum(hour => hour.SolarGainsW) / 1000.0,
                InternalGainsKWh: group.Sum(hour => hour.InternalGainsW) / 1000.0,
                TotalGainsKWh: group.Sum(hour => hour.TotalGainsW) / 1000.0,
                PeakHeatingLoadW: group.Max(hour => hour.HeatingLoadW),
                PeakCoolingLoadW: group.Max(hour => hour.CoolingLoadW),
                AverageIndoorTemperatureC: group.Average(hour => hour.AverageIndoorTemperatureC),
                AverageOutdoorTemperatureC: group.Average(hour => hour.OutdoorTemperatureC)))
            .ToArray();

    private static Result Validate(
        Iso52016BuildingSimulationFacadeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingCode))
            return Result.Validation("Building code is required.");

        if (!Enum.IsDefined(request.SimulationEngine))
            return Result.Validation("Unsupported ISO 52016 simulation engine.");

        if (request.Rooms is null)
            return Result.Validation("Building room list is required.");

        if (request.Rooms.Count == 0)
            return Result.Validation("Building must contain at least one room.");

        if (request.Rooms.Any(room => room is null))
            return Result.Validation("Building room list must not contain null rooms.");

        var duplicateRoomNames = request.Rooms
            .GroupBy(room => room.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (duplicateRoomNames.Length > 0)
        {
            return Result.Conflict(
                $"Room names must be unique inside building simulation request: {string.Join(", ", duplicateRoomNames)}.");
        }

        if (request.AnnualClimateData is null)
            return Result.Validation("Annual climate data is required.");

        if (request.LatitudeDegrees is < -90.0 or > 90.0)
            return Result.Validation("Latitude must be between -90 and 90 degrees.");

        if (request.LongitudeDegrees is < -180.0 or > 180.0)
            return Result.Validation("Longitude must be between -180 and 180 degrees.");

        if (request.GroundReflectance is < 0.0 or > 1.0)
            return Result.Validation("Ground reflectance must be between 0 and 1.");

        if (request.CoolingSetpointOverrideC.HasValue &&
            request.HeatingSetpointOverrideC.HasValue &&
            request.CoolingSetpointOverrideC.Value <= request.HeatingSetpointOverrideC.Value)
        {
            return Result.Validation(
                "Cooling setpoint override must be greater than heating setpoint override.");
        }

        return Result.Success();
    }
}