using System.Diagnostics.CodeAnalysis;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions.Results;

public static class ResultExtensions
{
    private const string GetByIdActionName = "GetById";

    public static ActionResult ToOkResult(
        this Result result,
        ControllerBase controller) =>
        result.IsSuccess
            ? new OkResult()
            : ErrorResult(controller, result);

    public static ActionResult<T> ToOkResult<T>(
        this Result<T> result,
        ControllerBase controller) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ErrorResult(controller, result);

    public static ActionResult<T> ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ErrorResult(controller, result);

    public static ActionResult ToActionResult(
        this Result result,
        ControllerBase controller) =>
        result.IsSuccess
            ? new NoContentResult()
            : ErrorResult(controller, result);

    public static ActionResult ToNoContentResult(
        this Result result,
        ControllerBase controller) =>
        result.IsSuccess
            ? new NoContentResult()
            : ErrorResult(controller, result);

    public static ActionResult<T> ToCreatedAtGetByIdResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, int> idSelector) =>
        result.ToCreatedResult(
            controller,
            GetByIdActionName,
            idSelector);

    public static ActionResult<T> ToCreatedResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string actionName,
        Func<T, int> idSelector)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(actionName);
        ArgumentNullException.ThrowIfNull(idSelector);

        if (result.IsFailure)
            return ErrorResult(controller, result);

        var routeValues = new
        {
            id = idSelector(result.Value)
        };

        return new CreatedAtActionResult(
            actionName,
            controllerName: null,
            routeValues,
            result.Value);
    }

    public static ActionResult ToFailureResult(
        this Result result,
        ControllerBase controller)
    {
        if (result.IsSuccess)
            ThrowSuccessfulFailureConversion();

        return ErrorResult(controller, result);
    }

    private static ObjectResult ErrorResult(
        ControllerBase controller,
        Result result) =>
        ApiProblemDetailsFactory.CreateResult(
            controller.HttpContext,
            result);

    [DoesNotReturn]
    private static void ThrowSuccessfulFailureConversion() =>
        throw new InvalidOperationException(
            "Cannot convert a successful result to a failure response.");
}