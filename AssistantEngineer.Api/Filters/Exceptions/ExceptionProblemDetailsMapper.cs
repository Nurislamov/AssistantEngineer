using AssistantEngineer.Api.Extensions.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Filters.Exceptions;

internal sealed class ExceptionProblemDetailsMapper : IExceptionProblemDetailsMapper
{
    private const string GenericErrorDetail =
        "An unexpected error occurred. Use the correlation id when contacting support.";

    private readonly IHostEnvironment _environment;

    public ExceptionProblemDetailsMapper(
        IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ObjectResult Map(
        HttpContext httpContext,
        Exception exception)
    {
        var problemDetails = ApiProblemDetailsFactory.CreateProblemDetails(
            httpContext,
            StatusCodes.Status500InternalServerError,
            code: "unexpected_error",
            title: "An unexpected error occurred.",
            detail: GenericErrorDetail);

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] =
                exception.GetType().Name;
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
}