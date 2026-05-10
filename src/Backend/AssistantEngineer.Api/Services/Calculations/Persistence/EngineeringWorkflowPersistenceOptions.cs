namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowPersistenceOptions
{
    public const string SectionName = "EngineeringWorkflowPersistence";

    public EngineeringWorkflowPersistenceProvider Provider { get; set; } = EngineeringWorkflowPersistenceProvider.InMemory;

    public string? SqliteConnectionString { get; set; }

    public bool EnsureCreatedOnStartup { get; set; } = true;
}
