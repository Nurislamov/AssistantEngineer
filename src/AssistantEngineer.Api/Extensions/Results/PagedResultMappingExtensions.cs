using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions.Results;

public static class PagedResultMappingExtensions
{
    public static ActionResult<PagedResponse<TItem>> ToPagedOkResult<TItem>(
        this Result<IReadOnlyList<TItem>> result,
        ControllerBase controller,
        CollectionQueryParameters query,
        Func<IEnumerable<TItem>, IEnumerable<TItem>> queryPipeline)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(queryPipeline);

        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(
                controller.HttpContext,
                result);

        var items = queryPipeline(result.Value);

        return controller.Ok(
            items.ToPagedResponse(query));
    }

    public static ActionResult<PagedResponse<TItem>> ToPagedOkResult<TItem>(
        this Result<List<TItem>> result,
        ControllerBase controller,
        CollectionQueryParameters query,
        Func<IEnumerable<TItem>, IEnumerable<TItem>> queryPipeline)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(queryPipeline);

        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(
                controller.HttpContext,
                result);

        var items = queryPipeline(result.Value);

        return controller.Ok(
            items.ToPagedResponse(query));
    }

    public static ActionResult<PagedResponse<TItem>> ToPagedOkResult<TItem>(
        this Result<IReadOnlyCollection<TItem>> result,
        ControllerBase controller,
        CollectionQueryParameters query,
        Func<IEnumerable<TItem>, IEnumerable<TItem>> queryPipeline)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(queryPipeline);

        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(
                controller.HttpContext,
                result);

        var items = queryPipeline(result.Value);

        return controller.Ok(
            items.ToPagedResponse(query));
    }

    public static ActionResult<PagedResponse<TItem>> ToPagedOkResult<TItem>(
        this Result<IEnumerable<TItem>> result,
        ControllerBase controller,
        CollectionQueryParameters query,
        Func<IEnumerable<TItem>, IEnumerable<TItem>> queryPipeline)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(queryPipeline);

        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(
                controller.HttpContext,
                result);

        var items = queryPipeline(result.Value);

        return controller.Ok(
            items.ToPagedResponse(query));
    }
}