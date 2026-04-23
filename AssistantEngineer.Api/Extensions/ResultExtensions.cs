using System.Diagnostics.CodeAnalysis;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToOkResult(this Result result) =>
        result.IsSuccess
            ? new OkResult()
            : ErrorResult(result);

    public static ActionResult<T> ToOkResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ErrorResult(result);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ErrorResult(result);

    public static ActionResult<T> ToCreatedResult<T>(
        this Result<T> result,
        string actionName,
        Func<T, int> idSelector)
    {
        if (result.IsFailure)
            return ErrorResult(result);

        var routeValues = new { id = idSelector(result.Value) };
        return new CreatedAtActionResult(actionName, null, routeValues, result.Value);
    }

    public static ActionResult ToFailureResult(this Result result)
    {
        if (result.IsSuccess)
            ThrowSuccessfulFailureConversion();

        return ErrorResult(result);
    }

    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => new NotFoundObjectResult(result.Error),
            ResultErrorType.Validation => new BadRequestObjectResult(result.Error),
            ResultErrorType.Conflict => new ConflictObjectResult(result.Error),
            _ => new BadRequestObjectResult(result.Error)
        };
    }
    
    private static ObjectResult ErrorResult(Result result) =>
        ErrorResult(result.Error, GetStatusCode(result.ErrorType));

    private static int GetStatusCode(ResultErrorType errorType) =>
        errorType switch
        {
            ResultErrorType.NotFound => StatusCodes.Status404NotFound,
            ResultErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

    private static ObjectResult ErrorResult(string error, int statusCode) =>
        new(new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = error
        })
        {
            StatusCode = statusCode
        };

    private static string GetTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status404NotFound => "Not found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Request failed"
        };

    [DoesNotReturn]
    private static void ThrowSuccessfulFailureConversion() =>
        throw new InvalidOperationException("Cannot convert a successful result to a failure response.");
}
