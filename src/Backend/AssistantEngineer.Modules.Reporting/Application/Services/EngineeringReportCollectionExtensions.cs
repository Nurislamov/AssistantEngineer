namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal static class EngineeringReportCollectionExtensions
{
    public static void AddRange<T>(
        this ICollection<T> target,
        IEnumerable<T> source)
    {
        foreach (var item in source)
            target.Add(item);
    }
}
