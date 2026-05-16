using AssistantEngineer.Modules.Identity.Application.Contracts;
using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;
using AssistantEngineer.Modules.Identity.Application.Services.Audit;

namespace AssistantEngineer.Tests.Identity;

public sealed class AuditEventFactoryTests
{
    private readonly AuditEventFactory _factory = new();

    [Fact]
    public void CreateAuthenticationSucceeded_ProducesExpectedShape()
    {
        var request = _factory.CreateAuthenticationSucceeded(CreatePrincipal(), "corr-1", "req-1");

        Assert.Equal(AuditEventTypes.AuthenticationSucceeded, request.EventType);
        Assert.Equal(AuditEventCategory.Authentication, request.Category);
        Assert.Equal(AuditEventOutcome.Succeeded, request.Outcome);
        Assert.NotNull(request.Principal);
        Assert.Equal("corr-1", request.CorrelationId);
    }

    [Fact]
    public void CreateAuthenticationFailed_ProducesExpectedShape()
    {
        var request = _factory.CreateAuthenticationFailed("InvalidApiKey", "corr-2", "req-2");

        Assert.Equal(AuditEventTypes.AuthenticationFailed, request.EventType);
        Assert.Equal(AuditEventCategory.Authentication, request.Category);
        Assert.Equal(AuditEventOutcome.Failed, request.Outcome);
        Assert.Equal("InvalidApiKey", request.FailureReason);
        Assert.Null(request.Principal);
    }

    [Fact]
    public void CreateAuthorizationDenied_ProducesExpectedShape()
    {
        var request = _factory.CreateAuthorizationDenied(
            CreatePrincipal(),
            resourceType: "Project",
            resourceId: "42",
            permission: "ProjectsWrite",
            failureReason: "TenantMismatch",
            correlationId: "corr-authz");

        Assert.Equal(AuditEventTypes.AuthorizationDenied, request.EventType);
        Assert.Equal(AuditEventCategory.Authorization, request.Category);
        Assert.Equal(AuditEventOutcome.Denied, request.Outcome);
        Assert.Equal("Project", request.ResourceType);
        Assert.Equal("42", request.ResourceId);
        Assert.Equal("ProjectsWrite", request.Permission);
    }

    [Fact]
    public void FactoryRequests_DoNotContainPayloadOrSecretMetadata()
    {
        var request = _factory.CreateReportGenerated(CreatePrincipal(), reportId: "rep-1", workflowId: "wf-1");

        Assert.Null(request.Metadata);
        Assert.Null(request.Permission);
        Assert.Null(request.FailureReason);
        Assert.Equal(AuditEventTypes.ReportGenerated, request.EventType);
    }

    private static PrincipalAccessContext CreatePrincipal()
    {
        return new PrincipalAccessContext(
            UserId: 10,
            OrganizationId: 20,
            ExternalSubjectId: "sub-10",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Engineer" },
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ProjectsRead", "ReportsRead" },
            IsAuthenticated: true);
    }
}
