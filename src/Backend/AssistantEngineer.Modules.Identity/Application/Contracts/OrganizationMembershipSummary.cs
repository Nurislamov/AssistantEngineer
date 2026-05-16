namespace AssistantEngineer.Modules.Identity.Application.Contracts;

public sealed record OrganizationMembershipSummary(
    int MembershipId,
    int OrganizationId,
    int UserId,
    string Role,
    bool IsActive);
