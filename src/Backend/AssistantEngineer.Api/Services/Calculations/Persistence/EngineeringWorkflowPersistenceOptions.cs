namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowPersistenceOptions
{
    public const string SectionName = "EngineeringWorkflowPersistence";

    public EngineeringWorkflowPersistenceProvider Provider { get; set; } = EngineeringWorkflowPersistenceProvider.InMemory;

    public string? SqliteConnectionString { get; set; }

    // Historical option name kept for configuration compatibility.
    // For the SQLite provider this now applies EF Core migrations, not EnsureCreated().
    public bool EnsureCreatedOnStartup { get; set; } = true;
}