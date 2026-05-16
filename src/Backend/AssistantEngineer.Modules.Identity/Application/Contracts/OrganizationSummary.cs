namespace AssistantEngineer.Modules.Identity.Application.Contracts;

public sealed record OrganizationSummary(
    int OrganizationId,
    string Name,
    string Slug,
    bool IsActive);
