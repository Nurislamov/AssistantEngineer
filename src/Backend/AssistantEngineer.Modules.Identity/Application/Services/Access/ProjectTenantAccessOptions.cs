namespace AssistantEngineer.Modules.Identity.Application.Services.Access;

public sealed class ProjectTenantAccessOptions
{
    public bool AllowUnscopedProjectsDuringTransition { get; set; } = true;
    public bool TreatMissingTenantAsBlocking { get; set; } = false;
    public bool EnableStrictTenantMatch { get; set; } = true;
}
