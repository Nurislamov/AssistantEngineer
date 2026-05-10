namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed record EngineeringWorkflowPersistenceProviderInfo(
    EngineeringWorkflowPersistenceProvider Provider,
    bool DurableEnabled,
    string ProviderLabel);
