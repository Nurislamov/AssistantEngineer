using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions.Http;

public static class ApiProblemDetailsFactory
{
    public static ObjectResult CreateResult(
        HttpContext httpContext,
        Result result)
    {
        var descriptor = ProblemDetailsErrorMapper.Map(
            result.ErrorType);

        return CreateProblemResult(
            httpContext,
            descriptor.StatusCode,
            descriptor.Code,
            descriptor.Title,
            result.Error);
    }

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
        string detail = "One or more validation errors occurred.") =>
        ApiValidationProblemDetailsFactory.CreateResult(
            context,
            detail);

    public static BadRequestObjectResult CreateValidationResult(
        ControllerBase controller,
        string detail,
        string field,
        params string[] errors)
    {
        controller.ModelState.AddModelError(
            field,
            errors.FirstOrDefault() ?? detail);

        return CreateValidationResult(
            controller.ControllerContext,
            detail);
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

        problemDetails.AddAssistantEngineerMetadata(
            httpContext,
            code);

        return problemDetails;
    }
}