using AssistantEngineer.Api.Contracts.Common;

namespace AssistantEngineer.Api.Extensions;

public static class CollectionQueryExtensions
{
    public static IEnumerable<T> ApplySearch<T>(
        this IEnumerable<T> source,
        string? search,
        params Func<T, string?>[] selectors)
    {
        var term = Normalize(search);
        if (term is null || selectors.Length == 0)
            return source;

        return source.Where(item => selectors.Any(selector => Contains(selector(item), term)));
    }

    public static PagedResponse<T> ToPagedResponse<T>(
        this IEnumerable<T> source,
        CollectionQueryParameters query)
    {
        var page = query.GetPage();
        var pageSize = query.GetPageSize();
        var items = source.ToList();
        var totalCount = items.Count;
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<T>
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Search = query.GetSearchTerm(),
            SortBy = Normalize(query.SortBy),
            SortDescending = query.SortDescending
        };
    }

    private static bool Contains(string? source, string search) =>
        !string.IsNullOrWhiteSpace(source) &&
        source.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
