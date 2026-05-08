namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

internal static class SystemEnergyProfileHelper
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public const int HoursPerYear = 8760;
    public const int MonthsPerYear = 12;

    public static double[] Ensure8760(IReadOnlyList<double>? source)
    {
        if (source is null || source.Count == 0)
            return new double[HoursPerYear];

        if (source.Count == HoursPerYear)
            return source.ToArray();

        var result = new double[HoursPerYear];
        var length = Math.Min(source.Count, HoursPerYear);
        for (var index = 0; index < length; index++)
        {
            result[index] = double.IsFinite(source[index]) ? source[index] : 0.0;
        }

        return result;
    }

    public static double[] ExpandMonthlyToHourly(IReadOnlyList<double> monthlyLossKWh)
    {
        var hourly = new double[HoursPerYear];
        var offset = 0;
        for (var month = 0; month < MonthsPerYear; month++)
        {
            var hours = DaysPerMonth[month] * 24;
            var value = month < monthlyLossKWh.Count && double.IsFinite(monthlyLossKWh[month])
                ? Math.Max(0.0, monthlyLossKWh[month]) / hours
                : 0.0;

            for (var hour = 0; hour < hours; hour++)
            {
                hourly[offset + hour] = value;
            }

            offset += hours;
        }

        return hourly;
    }

    public static IReadOnlyList<double> AggregateMonthly(IReadOnlyList<double> hourlyValues)
    {
        var monthly = new double[MonthsPerYear];
        var offset = 0;
        for (var month = 0; month < MonthsPerYear; month++)
        {
            var hours = DaysPerMonth[month] * 24;
            monthly[month] = hourlyValues.Skip(offset).Take(hours).Sum();
            offset += hours;
        }

        return monthly;
    }

    public static bool IsValidProfile(
        IReadOnlyList<double>? values,
        int expectedLength,
        bool nonNegative = true)
    {
        if (values is null || values.Count != expectedLength)
            return false;

        return values.All(value => double.IsFinite(value) && (!nonNegative || value >= 0.0));
    }

    public static double[] SumProfiles(IEnumerable<IReadOnlyList<double>> profiles)
    {
        var result = new double[HoursPerYear];
        foreach (var profile in profiles)
        {
            var normalized = Ensure8760(profile);
            for (var hour = 0; hour < HoursPerYear; hour++)
            {
                result[hour] += normalized[hour];
            }
        }

        return result;
    }
}
