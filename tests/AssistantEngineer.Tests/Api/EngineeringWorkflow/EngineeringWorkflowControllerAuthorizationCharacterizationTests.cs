using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.EngineeringWorkflow;

public sealed class EngineeringWorkflowControllerAuthorizationCharacterizationTests
{
    [Fact]
    public async Task ExecutionProtectionEnabled_WithoutPrincipal_ReturnsUnauthorized()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions(
                ApiAuthenticationEnabled: true,
                ApiAuthenticationAllowAnonymousInDevelopment: false,
                ApiAuthorizationEnabled: true,
                EnableExecutionEndpointProtectionPilot: true,
                RequireWorkflowExecuteAuthorization: true,
                ApiAuthorizationAllowAnonymousInDevelopment: false));

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/prepare-calculation",
            EngineeringWorkflowControllerCharacterizationHelper.CreatePreparationRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExecutionProtectionEnabled_MissingPermission_ReturnsForbidden()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions(
                ApiAuthenticationEnabled: true,
                ApiAuthenticationAllowAnonymousInDevelopment: false,
                ApiAuthorizationEnabled: true,
                EnableExecutionEndpointProtectionPilot: true,
                RequireWorkflowExecuteAuthorization: true,
                ApiAuthorizationAllowAnonymousInDevelopment: false,
                PrincipalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Permission.WorkflowsRead.ToString()
                }));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            EngineeringWorkflowControllerCharacterizationHelper.HeaderName,
            EngineeringWorkflowControllerCharacterizationHelper.ValidApiKey);

        var response = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/prepare-calculation",
            EngineeringWorkflowControllerCharacterizationHelper.CreatePreparationRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkflowReadTenantMismatch_WithNotFoundOptionEnabled_ReturnsNotFound()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions(
                ApiAuthenticationEnabled: true,
                ApiAuthenticationAllowAnonymousInDevelopment: false,
                ApiAuthorizationEnabled: true,
                EnableWorkflowReadEndpointProtectionPilot: true,
                RequireWorkflowReadAuthorization: true,
                ReturnNotFoundForTenantMismatch: true,
                ApiAuthorizationAllowAnonymousInDevelopment: false,
                PrincipalOrganizationId: 2001,
                ProjectScopeOrganizationId: 3001,
                BuildingScopeOrganizationId: 3001,
                PrincipalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Permission.WorkflowsRead.ToString()
                }));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            EngineeringWorkflowControllerCharacterizationHelper.HeaderName,
            EngineeringWorkflowControllerCharacterizationHelper.ValidApiKey);

        var response = await client.GetAsync("/api/v1/engineering-workflow/1/state?buildingId=11");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReportAndArtifactPolicies_RemainSeparatedFromWorkflowReadPermission()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions(
                ApiAuthenticationEnabled: true,
                ApiAuthenticationAllowAnonymousInDevelopment: false,
                ApiAuthorizationEnabled: true,
                EnableReportArtifactEndpointProtectionPilot: true,
                RequireReportReadAuthorization: true,
                RequireReportWriteAuthorization: true,
                RequireArtifactReadAuthorization: true,
                ApiAuthorizationAllowAnonymousInDevelopment: false,
                PrincipalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Permission.WorkflowsRead.ToString()
                }));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            EngineeringWorkflowControllerCharacterizationHelper.HeaderName,
            EngineeringWorkflowControllerCharacterizationHelper.ValidApiKey);

        var reportWriteResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report",
            new EngineeringWorkflowReportRequestDto(EngineeringWorkflowControllerCharacterizationHelper.CreateWorkflowState()));
        Assert.Equal(HttpStatusCode.Forbidden, reportWriteResponse.StatusCode);

        var artifactReadResponse = await client.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent/artifacts");
        Assert.Equal(HttpStatusCode.Forbidden, artifactReadResponse.StatusCode);
    }
}
