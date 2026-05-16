namespace AssistantEngineer.Modules.Identity.Application.Contracts;

public sealed record IdentityUserSummary(
    int UserId,
    string Email,
    string DisplayName,
    bool IsActive);
