namespace AssistantEngineer.Api.Contracts.Common;

public class CollectionQueryParameters
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = DefaultPage;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }

    public int GetPage() => Page < 1 ? DefaultPage : Page;

    public int GetPageSize()
    {
        var requestedPageSize = PageSize < 1 ? DefaultPageSize : PageSize;
        return Math.Min(requestedPageSize, MaxPageSize);
    }

    public string? GetSearchTerm() =>
        string.IsNullOrWhiteSpace(Search)
            ? null
            : Search.Trim();
}
