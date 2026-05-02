namespace AssistantEngineer.Api.Extensions.Http;

internal sealed record ProblemDetailsErrorDescriptor(
    int StatusCode,
    string Code,
    string Title);