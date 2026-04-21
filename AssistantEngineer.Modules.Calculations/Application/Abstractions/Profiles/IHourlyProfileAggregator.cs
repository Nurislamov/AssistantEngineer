namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IHourlyProfileAggregator
{
    List<double> SumProfiles(
        IEnumerable<IReadOnlyList<double>> profiles,
        CancellationToken cancellationToken = default);

    int FindPeakHour(IReadOnlyList<double> hourlyHeatLoadW);
}