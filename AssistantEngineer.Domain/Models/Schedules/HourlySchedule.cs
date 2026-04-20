using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Domain.Models.Schedules;

public class HourlySchedule
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyList<double> Factors { get; private set; } = Array.Empty<double>();

    private HourlySchedule() { }

    private HourlySchedule(string name, IReadOnlyList<double> factors)
    {
        Name = name;
        Factors = factors;
    }

    public static Result<HourlySchedule> Create(string name, IReadOnlyList<double> factors)
    {
        var nameResult = name.ToRequiredTrimmed("Schedule name");
        if (nameResult.IsFailure) return Result<HourlySchedule>.Failure(nameResult);

        if (factors.Count != 24)
            return Result<HourlySchedule>.Validation("Hourly schedule must contain exactly 24 factors.");

        if (factors.Any(factor => factor is < 0 or > 1))
            return Result<HourlySchedule>.Validation("Hourly schedule factors must be between 0 and 1.");

        return Result<HourlySchedule>.Success(new HourlySchedule(nameResult.Value, factors.ToArray()));
    }
}
