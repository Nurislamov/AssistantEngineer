using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;

public sealed class HourlyProfileAggregator : IHourlyProfileAggregator
{
    public List<double> SumProfiles(
        IEnumerable<IReadOnlyList<double>> profiles,
        CancellationToken cancellationToken = default)
    {
        Span<double> totals = stackalloc double[24];

        foreach (var profile in profiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var hour = 0; hour < Math.Min(24, profile.Count); hour++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totals[hour] += profile[hour];
            }
        }

        var result = new List<double>(capacity: 24);
        for (var hour = 0; hour < totals.Length; hour++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(Round(totals[hour]));
        }

        return result;
    }

    public int FindPeakLoadHourIndex(IReadOnlyList<double> hourlyHeatLoadW)
    {
        if (hourlyHeatLoadW.Count == 0)
            return 0;

        var peakHour = 0;
        var peakLoad = hourlyHeatLoadW[0];

        for (var hour = 1; hour < hourlyHeatLoadW.Count; hour++)
        {
            if (hourlyHeatLoadW[hour] <= peakLoad)
                continue;

            peakLoad = hourlyHeatLoadW[hour];
            peakHour = hour;
        }

        return peakHour;
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
