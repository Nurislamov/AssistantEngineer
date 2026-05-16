namespace AssistantEngineer.Api.Security.Authentication;

public sealed record ApiKeyValidationResult(
    bool IsValid,
    AuthenticatedPrincipal? Principal,
    string? FailureReasonCode)
{
    public static ApiKeyValidationResult Success(AuthenticatedPrincipal principal) =>
        new(true, principal, null);

    public static ApiKeyValidationResult Failure(string code) =>
        new(false, null, code);
}
