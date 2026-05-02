namespace AssistantEngineer.Api.Contracts.Common;

public class CollectionQueryParameters
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public int Page { get; set; } = DefaultPage;

    public int PageSize { get; set; } = DefaultPageSize;

    public int GetPage() =>
        Page <= 0
            ? DefaultPage
            : Page;

    public int GetPageSize()
    {
        if (PageSize <= 0)
            return DefaultPageSize;

        return Math.Min(PageSize, MaxPageSize);
    }

    public string? GetSearchTerm() =>
        string.IsNullOrWhiteSpace(Search)
            ? null
            : Search.Trim();
}