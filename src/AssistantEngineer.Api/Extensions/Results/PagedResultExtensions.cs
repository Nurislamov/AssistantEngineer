using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions.Results;

public static class PagedResultExtensions
{
    public static ActionResult<PagedResponse<T>> ToPagedOkResult<T>(
        this IEnumerable<T> source,
        ControllerBase controller,
        CollectionQueryParameters query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(query);

        return controller.Ok(
            source.ToPagedResponse(query));
    }
}