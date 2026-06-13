using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public sealed class OperationalCorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationalCorrelationIdMiddleware> _logger;
    private readonly OperationalCorrelationOptions _options;

    public OperationalCorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<OperationalCorrelationIdMiddleware> logger,
        IOptions<OperationalCorrelationOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOperationalCorrelationIdAccessor accessor)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var supplied = context.Request.Headers[_options.HeaderName].FirstOrDefault();
        var correlationId = OperationalCorrelationIdPolicy.IsValid(supplied, _options.MaxLength)
            ? supplied!
            : OperationalCorrelationIdPolicy.Create();
        var safePath = OperationalCorrelationIdPolicy.GetSafeRequestPath(context.Request);
        accessor.CorrelationId = correlationId;
        context.Response.Headers[_options.HeaderName] = correlationId;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["requestPath"] = safePath,
            ["requestMethod"] = context.Request.Method
        });
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Request started {RequestMethod} {RequestPath}",
            context.Request.Method,
            safePath);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Request completed {RequestMethod} {RequestPath} with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                safePath,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
            accessor.CorrelationId = null;
        }
    }
}
