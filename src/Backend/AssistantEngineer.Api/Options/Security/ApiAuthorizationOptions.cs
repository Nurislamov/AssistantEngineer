namespace AssistantEngineer.Api.Options.Security;

public sealed class ApiAuthorizationOptions
{
    public const string SectionName = "ApiAuthorization";

    public bool Enabled { get; set; } = false;

    public bool EnableEndpointProtectionPilot { get; set; } = false;

    public bool EnableReadEndpointProtectionPilot { get; set; } = false;

    public bool RequireProjectReadAuthorization { get; set; } = false;

    public bool RequireBuildingReadAuthorization { get; set; } = false;

    public bool EnableWriteEndpointProtectionPilot { get; set; } = false;

    public bool RequireProjectWriteAuthorization { get; set; } = false;

    public bool RequireBuildingWriteAuthorization { get; set; } = false;

    public bool EnableExecutionEndpointProtectionPilot { get; set; } = false;

    public bool RequireWorkflowExecuteAuthorization { get; set; } = false;

    public bool RequireCalculationRunAuthorization { get; set; } = false;

    public bool EnableReportArtifactEndpointProtectionPilot { get; set; } = false;

    public bool RequireReportReadAuthorization { get; set; } = false;

    public bool RequireReportWriteAuthorization { get; set; } = false;

    public bool RequireArtifactReadAuthorization { get; set; } = false;

    public bool RequireArtifactWriteAuthorization { get; set; } = false;

    public bool ReturnNotFoundForTenantMismatch { get; set; } = false;

    public bool AllowAnonymousInDevelopment { get; set; } = true;
}
