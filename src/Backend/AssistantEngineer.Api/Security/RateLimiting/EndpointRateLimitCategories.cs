namespace AssistantEngineer.Api.Security.RateLimiting;

public static class EndpointRateLimitCategories
{
    public const string PublicRead = "PublicRead";
    public const string ReferenceData = "ReferenceData";
    public const string ProjectRead = "ProjectRead";
    public const string ProjectWrite = "ProjectWrite";
    public const string BuildingRead = "BuildingRead";
    public const string BuildingWrite = "BuildingWrite";
    public const string WorkflowRead = "WorkflowRead";
    public const string WorkflowExecute = "WorkflowExecute";
    public const string CalculationRun = "CalculationRun";
    public const string ReportGenerate = "ReportGenerate";
    public const string ArtifactRead = "ArtifactRead";
    public const string ArtifactWrite = "ArtifactWrite";
    public const string Administration = "Administration";
}
