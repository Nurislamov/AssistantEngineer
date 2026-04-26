using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Extensions.Http;

internal static class ProblemDetailsErrorMapper
{
    public static ProblemDetailsErrorDescriptor Map(
        ResultErrorType errorType) =>
        errorType switch
        {
            ResultErrorType.NotFound => new ProblemDetailsErrorDescriptor(
                StatusCodes.Status404NotFound,
                "resource_not_found",
                "Not found"),

            ResultErrorType.Validation => new ProblemDetailsErrorDescriptor(
                StatusCodes.Status400BadRequest,
                "validation_failed",
                "Validation failed"),

            ResultErrorType.Conflict => new ProblemDetailsErrorDescriptor(
                StatusCodes.Status409Conflict,
                "conflict",
                "Conflict"),

            _ => new ProblemDetailsErrorDescriptor(
                StatusCodes.Status400BadRequest,
                "request_failed",
                "Request failed")
        };
}