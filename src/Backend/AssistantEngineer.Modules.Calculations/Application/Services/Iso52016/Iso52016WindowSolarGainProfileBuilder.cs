using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016WindowSolarGainProfileBuilder : ISo52016WindowSolarGainProfileBuilder
{
    private readonly ISo52016WindowSolarGainCalculator _calculator;

    public Iso52016WindowSolarGainProfileBuilder(
        ISo52016WindowSolarGainCalculator calculator)
    {
        _calculator = calculator;
    }

    public Result<Iso52016WindowSolarGainProfile> Build(
        Iso52016WindowSolarGainProfileRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016WindowSolarGainProfile>.Failure(validation);

        var hourlyResults = new List<Iso52016HourlyWindowSolarGainRecord>(
            request.WeatherSolarContext.HourCount);

        foreach (var hour in request.WeatherSolarContext.Hours)
        {
            var result = _calculator.Calculate(
                new Iso52016WindowSolarGainRequest(
                    Hour: hour,
                    Orientation: request.Orientation,
                    WindowAreaM2: request.WindowAreaM2,
                    SolarHeatGainCoefficient: request.SolarHeatGainCoefficient,
                    FrameFraction: request.FrameFraction,
                    ShadingFactor: request.ShadingFactor));

            if (result.IsFailure)
                return Result<Iso52016WindowSolarGainProfile>.Failure(result);

            hourlyResults.Add(
                new Iso52016HourlyWindowSolarGainRecord(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    Orientation: request.Orientation,
                    SurfaceCode: result.Value.SurfaceCode,
                    SolarGainW: result.Value.TotalSolarGainW));
        }

        return Result<Iso52016WindowSolarGainProfile>.Success(
            new Iso52016WindowSolarGainProfile(
                Orientation: request.Orientation,
                WindowAreaM2: request.WindowAreaM2,
                SolarHeatGainCoefficient: request.SolarHeatGainCoefficient,
                FrameFraction: request.FrameFraction,
                ShadingFactor: request.ShadingFactor,
                Hours: hourlyResults));
    }

    private static Result Validate(
        Iso52016WindowSolarGainProfileRequest request)
    {
        if (request.WeatherSolarContext is null)
            return Result.Validation("ISO 52016 weather-solar context is required.");

        if (request.WeatherSolarContext.HourCount == 0)
            return Result.Validation("ISO 52016 weather-solar context must contain hourly records.");

        if (request.WindowAreaM2 <= 0)
            return Result.Validation("Window area must be greater than zero.");

        if (request.SolarHeatGainCoefficient is < 0.0 or > 1.0)
            return Result.Validation("Solar heat gain coefficient must be between 0 and 1.");

        if (request.FrameFraction is < 0.0 or >= 1.0)
            return Result.Validation("Frame fraction must be greater than or equal to 0 and less than 1.");

        if (request.ShadingFactor is < 0.0 or > 1.0)
            return Result.Validation("Shading factor must be between 0 and 1.");

        return Result.Success();
    }
}