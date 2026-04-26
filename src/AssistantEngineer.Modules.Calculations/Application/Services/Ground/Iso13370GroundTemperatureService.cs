using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class Iso13370GroundTemperatureService : IGroundTemperatureService
{
    private readonly Iso13370GroundTemperatureOptions _options;

    public Iso13370GroundTemperatureService(IOptions<Iso13370GroundTemperatureOptions> options)
    {
        _options = options.Value;
    }

    public double[] BuildHourlyProfile(IReadOnlyList<HourlyClimateData> hourlyClimateData)
    {
        if (hourlyClimateData.Count == 0)
            return Array.Empty<double>();

        var ordered = hourlyClimateData
            .Where(h => h.HourOfYear.HasValue)
            .OrderBy(h => h.HourOfYear!.Value)
            .ToArray();

        if (ordered.Length == 0)
            return Array.Empty<double>();

        var inferredYear = InferYearFromHourCount(ordered.Length);
        var daysInYear = DateTime.IsLeapYear(inferredYear) ? 366 : 365;
        var hoursInYear = daysInYear * 24;

        var monthlyMeans = BuildMonthlyOutdoorMeans(ordered, inferredYear);
        var annualMeanOutdoor = monthlyMeans.Average();
        var annualMeanGround = annualMeanOutdoor + _options.MeanTemperatureOffsetC;

        var outdoorAmplitude = (monthlyMeans.Max() - monthlyMeans.Min()) / 2.0;
        var groundAmplitude = outdoorAmplitude * _options.AmplitudeAttenuationFactor;

        var coldestMonth = Array.IndexOf(monthlyMeans, monthlyMeans.Min()) + 1;
        var coldestMonthMidDay = GetApproximateMonthMidDayOfYear(inferredYear, coldestMonth);
        var shiftedMinDay = coldestMonthMidDay + _options.PhaseShiftDays;

        var result = new double[hoursInYear];

        for (var hour = 0; hour < hoursInYear; hour++)
        {
            var dayOfYear = hour / 24.0 + 1.0;
            var seasonal = Math.Cos(2.0 * Math.PI * (dayOfYear - shiftedMinDay) / daysInYear);
            var temperature = annualMeanGround - groundAmplitude * seasonal;

            result[hour] = Clamp(temperature);
        }

        return result;
    }

    public double GetMonthlyAverageTemperature(IReadOnlyList<HourlyClimateData> hourlyClimateData, int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        var ordered = hourlyClimateData
            .Where(h => h.HourOfYear.HasValue)
            .OrderBy(h => h.HourOfYear!.Value)
            .ToArray();

        if (ordered.Length == 0)
            return Clamp(_options.MeanTemperatureOffsetC + 10.0);

        var profile = BuildHourlyProfile(ordered);
        if (profile.Length == 0)
            return Clamp(_options.MeanTemperatureOffsetC + 10.0);

        var inferredYear = InferYearFromHourCount(profile.Length);
        var yearStart = new DateTime(inferredYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthStart = new DateTime(inferredYear, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = month == 12
            ? new DateTime(inferredYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(inferredYear, month + 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var fromHour = (int)(monthStart - yearStart).TotalHours;
        var toHour = Math.Min((int)(monthEnd - yearStart).TotalHours, profile.Length);

        if (fromHour >= toHour)
            return Clamp(profile[Math.Clamp(fromHour, 0, profile.Length - 1)]);

        return Clamp(profile.Skip(fromHour).Take(toHour - fromHour).Average());
    }

    private static double[] BuildMonthlyOutdoorMeans(
        IReadOnlyList<HourlyClimateData> ordered,
        int year)
    {
        var buckets = Enumerable.Range(1, 12)
            .ToDictionary(month => month, _ => new List<double>());

        foreach (var hour in ordered)
        {
            if (!hour.HourOfYear.HasValue)
                continue;

            var timestamp = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddHours(hour.HourOfYear.Value);

            buckets[timestamp.Month].Add(hour.DryBulbTemperature);
        }

        return Enumerable.Range(1, 12)
            .Select(month => buckets[month].Count == 0 ? 0.0 : buckets[month].Average())
            .ToArray();
    }

    private static int GetApproximateMonthMidDayOfYear(int year, int month)
    {
        var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var next = month == 12
            ? new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(year, month + 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var mid = first.AddDays((next - first).TotalDays / 2.0);
        return mid.DayOfYear;
    }

    private static int InferYearFromHourCount(int hourCount) =>
        hourCount >= 8784 ? 2024 : 2025;

    private double Clamp(double value) =>
        Math.Clamp(value, _options.MinimumGroundTemperatureC, _options.MaximumGroundTemperatureC);
}