using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions.Http;

internal static class ApiValidationProblemDetailsFactory
{
    public static BadRequestObjectResult CreateResult(
        ActionContext context,
        string detail)
    {
        var problemDetails = new ValidationProblemDetails(
            context.ModelState)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail,
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.AddAssistantEngineerMetadata(
            context.HttpContext,
            code: "validation_failed");

        return new BadRequestObjectResult(problemDetails);
    }
}