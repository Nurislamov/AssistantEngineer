using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AssistantEngineer.Api.Extensions;

namespace AssistantEngineer.Api.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private const string GenericErrorDetail = "An unexpected error occurred. Use the correlation id when contacting support.";

    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception occurred.");
        var problemDetails = ApiProblemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            StatusCodes.Status500InternalServerError,
            code: "unexpected_error",
            title: "An unexpected error occurred.",
            detail: GenericErrorDetail);

        if (_environment.IsDevelopment())
            problemDetails.Extensions["exceptionType"] = context.Exception.GetType().Name;

        context.Result = new ObjectResult(problemDetails) { StatusCode = StatusCodes.Status500InternalServerError };
        context.ExceptionHandled = true;
    }
}
