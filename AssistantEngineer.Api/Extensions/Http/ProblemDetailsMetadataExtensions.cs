using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Extensions.Http;

internal static class ProblemDetailsMetadataExtensions
{
    public const string CodeExtensionName = "code";
    public const string CorrelationIdExtensionName = "correlationId";
    public const string TraceIdExtensionName = "traceId";

    public static void AddAssistantEngineerMetadata(
        this ProblemDetails problemDetails,
        HttpContext httpContext,
        string code)
    {
        problemDetails.Extensions[CodeExtensionName] = code;
        problemDetails.Extensions[CorrelationIdExtensionName] = httpContext.TraceIdentifier;
        problemDetails.Extensions[TraceIdExtensionName] = httpContext.TraceIdentifier;
    }
}