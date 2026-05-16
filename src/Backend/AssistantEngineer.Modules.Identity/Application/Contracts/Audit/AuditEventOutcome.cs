namespace AssistantEngineer.Modules.Identity.Application.Contracts.Audit;

public enum AuditEventOutcome
{
    Succeeded = 0,
    Failed = 1,
    Denied = 2,
    Skipped = 3,
    Unknown = 4
}
