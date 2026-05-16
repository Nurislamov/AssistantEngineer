using AssistantEngineer.Modules.Identity.Application.Contracts;
using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;

namespace AssistantEngineer.Modules.Identity.Application.Services.Audit;

public sealed class AuditEventFactory
{
    public AuditEventWriteRequest CreateAuthenticationSucceeded(
        PrincipalAccessContext? principal,
        string? correlationId = null,
        string? requestId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.AuthenticationSucceeded,
            Category: AuditEventCategory.Authentication,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: requestId,
            ResourceType: null,
            ResourceId: null,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateAuthenticationFailed(
        string failureReason,
        string? correlationId = null,
        string? requestId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.AuthenticationFailed,
            Category: AuditEventCategory.Authentication,
            Outcome: AuditEventOutcome.Failed,
            Principal: null,
            CorrelationId: correlationId,
            RequestId: requestId,
            ResourceType: null,
            ResourceId: null,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: string.IsNullOrWhiteSpace(failureReason) ? "UnknownFailure" : failureReason.Trim(),
            Metadata: null);
    }

    public AuditEventWriteRequest CreateAuthorizationDenied(
        PrincipalAccessContext? principal,
        string resourceType,
        string resourceId,
        string permission,
        string failureReason,
        string? correlationId = null,
        string? requestId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.AuthorizationDenied,
            Category: AuditEventCategory.Authorization,
            Outcome: AuditEventOutcome.Denied,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: requestId,
            ResourceType: resourceType,
            ResourceId: resourceId,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: permission,
            FailureReason: failureReason,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateWorkflowExecutionStarted(
        PrincipalAccessContext? principal,
        string workflowId,
        string? projectId = null,
        string? buildingId = null,
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.WorkflowExecutionStarted,
            Category: AuditEventCategory.Workflow,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: null,
            ResourceType: "Workflow",
            ResourceId: workflowId,
            ProjectId: projectId,
            BuildingId: buildingId,
            WorkflowId: workflowId,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateWorkflowExecutionCompleted(
        PrincipalAccessContext? principal,
        string workflowId,
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.WorkflowExecutionCompleted,
            Category: AuditEventCategory.Workflow,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: null,
            ResourceType: "Workflow",
            ResourceId: workflowId,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: workflowId,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateCalculationStarted(
        PrincipalAccessContext? principal,
        string resourceType,
        string resourceId,
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.CalculationStarted,
            Category: AuditEventCategory.Calculation,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: null,
            ResourceType: resourceType,
            ResourceId: resourceId,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateCalculationCompleted(
        PrincipalAccessContext? principal,
        string resourceType,
        string resourceId,
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.CalculationCompleted,
            Category: AuditEventCategory.Calculation,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: null,
            ResourceType: resourceType,
            ResourceId: resourceId,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateArtifactWritten(
        PrincipalAccessContext? principal,
        string artifactId,
        string resourceType,
        string resourceId,
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.ArtifactWritten,
            Category: AuditEventCategory.Artifact,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: null,
            ResourceType: resourceType,
            ResourceId: resourceId,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: artifactId,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    public AuditEventWriteRequest CreateReportGenerated(
        PrincipalAccessContext? principal,
        string reportId,
        string? workflowId = null,
        string? correlationId = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.ReportGenerated,
            Category: AuditEventCategory.Report,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: principal,
            CorrelationId: correlationId,
            RequestId: null,
            ResourceType: "Report",
            ResourceId: reportId,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: workflowId,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }
}
