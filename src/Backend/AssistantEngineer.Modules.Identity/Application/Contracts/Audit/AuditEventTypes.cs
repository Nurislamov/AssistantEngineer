namespace AssistantEngineer.Modules.Identity.Application.Contracts.Audit;

public static class AuditEventTypes
{
    public const string AuthenticationSucceeded = "AUD-AUTH-001";
    public const string AuthenticationFailed = "AUD-AUTH-002";

    public const string AuthorizationSucceeded = "AUD-AUTHZ-001";
    public const string AuthorizationDenied = "AUD-AUTHZ-002";

    public const string ProjectCreated = "AUD-PROJ-001";
    public const string ProjectUpdated = "AUD-PROJ-002";
    public const string ProjectDeleted = "AUD-PROJ-003";

    public const string BuildingCreated = "AUD-BLD-001";
    public const string BuildingUpdated = "AUD-BLD-002";
    public const string BuildingDeleted = "AUD-BLD-003";

    public const string WorkflowExecutionStarted = "AUD-WF-001";
    public const string WorkflowExecutionCompleted = "AUD-WF-002";
    public const string WorkflowExecutionFailed = "AUD-WF-003";

    public const string CalculationStarted = "AUD-CALC-001";
    public const string CalculationCompleted = "AUD-CALC-002";
    public const string CalculationFailed = "AUD-CALC-003";

    public const string ReportGenerated = "AUD-REP-001";
    public const string ReportViewedOrExported = "AUD-REP-002";

    public const string ArtifactWritten = "AUD-ART-001";
    public const string ArtifactRead = "AUD-ART-002";
    public const string ArtifactDeleted = "AUD-ART-003";

    public const string OrganizationMembershipChanged = "AUD-ADM-001";
    public const string RoleChanged = "AUD-ADM-002";
}
