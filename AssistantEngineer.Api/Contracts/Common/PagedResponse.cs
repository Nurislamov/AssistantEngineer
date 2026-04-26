namespace AssistantEngineer.Api.Contracts.Common;

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}