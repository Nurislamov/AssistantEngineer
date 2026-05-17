using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

internal sealed record TenantIsolationScenario(
    string Group,
    Permission Permission,
    string RolloutStage,
    string TestMethod,
    bool UsesWorkflowReadGate = false,
    bool UsesWorkflowExecuteGate = false,
    bool UsesCalculationGate = false,
    bool UsesReportReadGate = false,
    bool UsesReportWriteGate = false,
    bool UsesArtifactReadGate = false,
    bool UsesProjectScope = false,
    bool UsesBuildingScope = false,
    bool UsesScenarioScope = false,
    bool UsesJobScope = false)
{
    public const int TenantAOrganizationId = 1001;
    public const int TenantBOrganizationId = 1002;
    public const int TenantAUserId = 2001;
    public const int TenantBUserId = 2002;
    public const int ProjectAId = 10;
    public const int BuildingAId = 20;
    public const int FloorAId = 30;
    public const int RoomAId = 40;
    public const string WorkflowAId = "workflow-a";
    public const string ScenarioAId = "scenario-a";
    public const string JobAId = "job-a";
    public const string ArtifactAId = "artifact-a";

    public static readonly IReadOnlyList<Permission> TenantPermissions =
    [
        Permission.ProjectsRead,
        Permission.ProjectsWrite,
        Permission.BuildingsRead,
        Permission.BuildingsWrite,
        Permission.WorkflowsRead,
        Permission.WorkflowsExecute,
        Permission.ReportsRead,
        Permission.ReportsWrite
    ];

    public static readonly IReadOnlyList<TenantIsolationScenario> EndpointGroups =
    [
        new("ProjectsRead", Permission.ProjectsRead, "P5-10", nameof(TenantIsolationAuthorizationGateMatrixTests.ProjectsRead_Matrix), UsesProjectScope: true),
        new("ProjectsWrite", Permission.ProjectsWrite, "P5-11", nameof(TenantIsolationAuthorizationGateMatrixTests.ProjectsWrite_Matrix), UsesProjectScope: true),
        new("BuildingsRead", Permission.BuildingsRead, "P5-10", nameof(TenantIsolationAuthorizationGateMatrixTests.BuildingsRead_Matrix), UsesBuildingScope: true),
        new("BuildingsWrite", Permission.BuildingsWrite, "P5-11", nameof(TenantIsolationAuthorizationGateMatrixTests.BuildingsWrite_Matrix), UsesBuildingScope: true),
        new("WorkflowsRead", Permission.WorkflowsRead, "P5-14", nameof(TenantIsolationAuthorizationGateMatrixTests.WorkflowsRead_Matrix), UsesWorkflowReadGate: true),
        new("WorkflowsExecute", Permission.WorkflowsExecute, "P5-12", nameof(TenantIsolationAuthorizationGateMatrixTests.WorkflowsExecute_Matrix), UsesWorkflowExecuteGate: true),
        new("CalculationRun", Permission.WorkflowsExecute, "P5-12", nameof(TenantIsolationAuthorizationGateMatrixTests.CalculationRun_Matrix), UsesCalculationGate: true),
        new("ReportsRead", Permission.ReportsRead, "P5-13", nameof(TenantIsolationAuthorizationGateMatrixTests.ReportsRead_Matrix), UsesReportReadGate: true),
        new("ReportsWrite", Permission.ReportsWrite, "P5-13", nameof(TenantIsolationAuthorizationGateMatrixTests.ReportsWrite_Matrix), UsesReportWriteGate: true),
        new("ArtifactRead", Permission.ReportsRead, "P5-13", nameof(TenantIsolationAuthorizationGateMatrixTests.ArtifactRead_Matrix), UsesArtifactReadGate: true),
        new("WorkflowScenarioRead", Permission.WorkflowsRead, "P5-14", nameof(TenantIsolationAuthorizationGateMatrixTests.WorkflowScenarioRead_Matrix), UsesWorkflowReadGate: true, UsesScenarioScope: true),
        new("WorkflowJobRead", Permission.WorkflowsRead, "P5-14", nameof(TenantIsolationAuthorizationGateMatrixTests.WorkflowJobRead_Matrix), UsesWorkflowReadGate: true, UsesJobScope: true),
        new("WorkflowJobEventsRead", Permission.WorkflowsRead, "P5-14", nameof(TenantIsolationAuthorizationGateMatrixTests.WorkflowJobEventsRead_Matrix), UsesWorkflowReadGate: true, UsesJobScope: true)
    ];
}
