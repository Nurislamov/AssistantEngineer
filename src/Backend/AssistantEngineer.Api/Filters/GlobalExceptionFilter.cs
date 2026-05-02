using AssistantEngineer.Api.Filters.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssistantEngineer.Api.Filters;

internal sealed class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IExceptionProblemDetailsMapper _problemDetailsMapper;

    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IExceptionProblemDetailsMapper problemDetailsMapper)
    {
        _logger = logger;
        _problemDetailsMapper = problemDetailsMapper;
    }

    public void OnException(
        ExceptionContext context)
    {
        _logger.LogError(
            context.Exception,
            "Unhandled exception occurred.");

        context.Result = _problemDetailsMapper.Map(
            context.HttpContext,
            context.Exception);

        context.ExceptionHandled = true;
    }
}