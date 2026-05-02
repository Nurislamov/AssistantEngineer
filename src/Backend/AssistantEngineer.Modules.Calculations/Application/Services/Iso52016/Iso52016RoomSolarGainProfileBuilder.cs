using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomSolarGainProfileBuilder : IIso52016RoomSolarGainProfileBuilder
{
    private readonly IIso52016WindowSolarGainCalculator _windowSolarGainCalculator;

    public Iso52016RoomSolarGainProfileBuilder(
        IIso52016WindowSolarGainCalculator windowSolarGainCalculator)
    {
        _windowSolarGainCalculator = windowSolarGainCalculator;
    }

    public Result<Iso52016RoomSolarGainProfile> Build(
        Iso52016RoomSolarGainProfileRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomSolarGainProfile>.Failure(validation);

        var hourlyRecords = new List<Iso52016HourlyRoomSolarGainRecord>(
            request.WeatherSolarContext.HourCount);

        foreach (var hour in request.WeatherSolarContext.Hours)
        {
            var hourlyResult = BuildHour(
                hour,
                request.Windows);

            if (hourlyResult.IsFailure)
                return Result<Iso52016RoomSolarGainProfile>.Failure(hourlyResult);

            hourlyRecords.Add(
                hourlyResult.Value);
        }

        return Result<Iso52016RoomSolarGainProfile>.Success(
            new Iso52016RoomSolarGainProfile(
                RoomCode: request.RoomCode.Trim(),
                Windows: request.Windows,
                Hours: hourlyRecords));
    }

    private Result<Iso52016HourlyRoomSolarGainRecord> BuildHour(
        Iso52016HourlyWeatherSolarRecord hour,
        IReadOnlyList<Iso52016WindowSolarGainInput> windows)
    {
        var windowRecords = new List<Iso52016HourlyRoomWindowSolarGainRecord>(
            windows.Count);

        foreach (var window in windows)
        {
            var result = _windowSolarGainCalculator.Calculate(
                new Iso52016WindowSolarGainRequest(
                    Hour: hour,
                    Orientation: window.Orientation,
                    WindowAreaM2: window.WindowAreaM2,
                    SolarHeatGainCoefficient: window.SolarHeatGainCoefficient,
                    FrameFraction: window.FrameFraction,
                    ShadingFactor: window.ShadingFactor));

            if (result.IsFailure)
                return Result<Iso52016HourlyRoomSolarGainRecord>.Failure(result);

            windowRecords.Add(
                new Iso52016HourlyRoomWindowSolarGainRecord(
                    WindowCode: window.WindowCode.Trim(),
                    Orientation: window.Orientation,
                    SurfaceCode: result.Value.SurfaceCode,
                    WindowAreaM2: result.Value.WindowAreaM2,
                    EffectiveGlazingAreaM2: result.Value.EffectiveGlazingAreaM2,
                    BeamSolarGainW: result.Value.BeamSolarGainW,
                    DiffuseSkySolarGainW: result.Value.DiffuseSkySolarGainW,
                    GroundReflectedSolarGainW: result.Value.GroundReflectedSolarGainW,
                    TotalSolarGainW: result.Value.TotalSolarGainW));
        }

        return Result<Iso52016HourlyRoomSolarGainRecord>.Success(
            new Iso52016HourlyRoomSolarGainRecord(
                HourOfYear: hour.HourOfYear,
                Month: hour.Month,
                Day: hour.Day,
                Hour: hour.Hour,
                BeamSolarGainW: windowRecords.Sum(window => window.BeamSolarGainW),
                DiffuseSkySolarGainW: windowRecords.Sum(window => window.DiffuseSkySolarGainW),
                GroundReflectedSolarGainW: windowRecords.Sum(window => window.GroundReflectedSolarGainW),
                TotalSolarGainW: windowRecords.Sum(window => window.TotalSolarGainW),
                WindowGains: windowRecords));
    }

    private static Result Validate(
        Iso52016RoomSolarGainProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomCode))
            return Result.Validation("Room code is required.");

        if (request.WeatherSolarContext is null)
            return Result.Validation("ISO 52016 weather-solar context is required.");

        if (request.WeatherSolarContext.HourCount == 0)
            return Result.Validation("ISO 52016 weather-solar context must contain hourly records.");

        if (request.Windows is null)
            return Result.Validation("Room window list is required.");

        var invalidWindowCodes = request.Windows
            .Where(window => string.IsNullOrWhiteSpace(window.WindowCode))
            .ToArray();

        if (invalidWindowCodes.Length > 0)
            return Result.Validation("Window code is required.");

        var duplicateWindowCodes = request.Windows
            .GroupBy(window => window.WindowCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (duplicateWindowCodes.Length > 0)
        {
            return Result.Conflict(
                $"Window codes must be unique inside room solar gain request: {string.Join(", ", duplicateWindowCodes)}.");
        }

        foreach (var window in request.Windows)
        {
            var windowValidation = ValidateWindow(
                window);

            if (windowValidation.IsFailure)
                return windowValidation;
        }

        return Result.Success();
    }

    private static Result ValidateWindow(
        Iso52016WindowSolarGainInput window)
    {
        if (window.WindowAreaM2 <= 0)
        {
            return Result.Validation(
                $"Window '{window.WindowCode}' area must be greater than zero.");
        }

        if (window.SolarHeatGainCoefficient is < 0.0 or > 1.0)
        {
            return Result.Validation(
                $"Window '{window.WindowCode}' solar heat gain coefficient must be between 0 and 1.");
        }

        if (window.FrameFraction is < 0.0 or >= 1.0)
        {
            return Result.Validation(
                $"Window '{window.WindowCode}' frame fraction must be greater than or equal to 0 and less than 1.");
        }

        if (window.ShadingFactor is < 0.0 or > 1.0)
        {
            return Result.Validation(
                $"Window '{window.WindowCode}' shading factor must be between 0 and 1.");
        }

        return Result.Success();
    }
}