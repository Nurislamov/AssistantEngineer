using AssistantEngineer.Api.Contracts.Common;

namespace AssistantEngineer.Api.Extensions.Collections;

public delegate IOrderedEnumerable<T> SortRule<T>(
    IEnumerable<T> source,
    bool descending);

public static class SortExtensions
{
    public static IEnumerable<T> ApplySort<T>(
        this IEnumerable<T> source,
        CollectionQueryParameters query,
        string defaultSortBy,
        IReadOnlyDictionary<string, SortRule<T>> sortRules)
    {
        if (sortRules.Count == 0)
            return source;

        var sortBy = Normalize(query.SortBy) ?? defaultSortBy;

        if (!sortRules.TryGetValue(sortBy, out var sortRule))
        {
            if (!sortRules.TryGetValue(defaultSortBy, out sortRule))
                return source;
        }

        return sortRule(
            source,
            query.SortDescending);
    }

    public static IOrderedEnumerable<T> SortBy<T, TKey>(
        this IEnumerable<T> source,
        bool descending,
        Func<T, TKey> keySelector) =>
        descending
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);

    public static IOrderedEnumerable<T> ThenByStable<T, TKey>(
        this IOrderedEnumerable<T> source,
        bool descending,
        Func<T, TKey> keySelector) =>
        descending
            ? source.ThenByDescending(keySelector)
            : source.ThenBy(keySelector);

    private static string? Normalize(
        string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
}