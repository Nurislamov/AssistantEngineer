using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Filters.Exceptions;

internal interface IExceptionProblemDetailsMapper
{
    ObjectResult Map(
        HttpContext httpContext,
        Exception exception);
}