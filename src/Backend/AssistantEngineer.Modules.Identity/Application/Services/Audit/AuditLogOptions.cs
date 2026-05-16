namespace AssistantEngineer.Modules.Identity.Application.Services.Audit;

public sealed class AuditLogOptions
{
    public const string SectionName = "Identity:AuditLog";

    public bool Enabled { get; set; }
    public bool WriteAuthorizationDeniedEvents { get; set; } = true;
    public bool WriteArtifactEvents { get; set; } = true;
    public int MaxMetadataValueLength { get; set; } = 512;
    public string Provider { get; set; } = "InMemory";
}
