using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions;

public static class ApiProblemDetailsFactory
{
    public const string CodeExtensionName = "code";
    public const string CorrelationIdExtensionName = "correlationId";
    public const string TraceIdExtensionName = "traceId";

    public static ObjectResult CreateResult(HttpContext httpContext, Result result) =>
        CreateProblemResult(
            httpContext,
            GetStatusCode(result.ErrorType),
            GetCode(result.ErrorType),
            GetTitle(result.ErrorType),
            result.Error);

    public static ObjectResult CreateProblemResult(
        HttpContext httpContext,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        var problemDetails = CreateProblemDetails(
            httpContext,
            statusCode,
            code,
            title,
            detail);

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    public static BadRequestObjectResult CreateValidationResult(
        ActionContext context,
        string detail = "One or more validation errors occurred.")
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = context.HttpContext.Request.Path
        };

        AddCommonExtensions(problemDetails, context.HttpContext, code: "validation_failed");
        return new BadRequestObjectResult(problemDetails);
    }

    public static BadRequestObjectResult CreateValidationResult(
        ControllerBase controller,
        string detail,
        string field,
        params string[] errors)
    {
        controller.ModelState.AddModelError(field, errors.FirstOrDefault() ?? detail);
        return CreateValidationResult(controller.ControllerContext, detail);
    }

    public static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"urn:assistant-engineer:error:{code}"
        };

        AddCommonExtensions(problemDetails, httpContext, code);
        return problemDetails;
    }

    public static void AddCommonExtensions(
        ProblemDetails problemDetails,
        HttpContext httpContext,
        string code)
    {
        problemDetails.Extensions[CodeExtensionName] = code;
        problemDetails.Extensions[CorrelationIdExtensionName] = httpContext.TraceIdentifier;
        problemDetails.Extensions[TraceIdExtensionName] = httpContext.TraceIdentifier;
    }

    private static int GetStatusCode(ResultErrorType errorType) =>
        errorType switch
        {
            ResultErrorType.NotFound => StatusCodes.Status404NotFound,
            ResultErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

    private static string GetCode(ResultErrorType errorType) =>
        errorType switch
        {
            ResultErrorType.NotFound => "resource_not_found",
            ResultErrorType.Validation => "validation_failed",
            ResultErrorType.Conflict => "conflict",
            _ => "request_failed"
        };

    private static string GetTitle(ResultErrorType errorType) =>
        errorType switch
        {
            ResultErrorType.NotFound => "Not found",
            ResultErrorType.Validation => "Validation failed",
            ResultErrorType.Conflict => "Conflict",
            _ => "Request failed"
        };
}
