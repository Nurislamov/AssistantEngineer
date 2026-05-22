namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;

public sealed record EngineeringWorkflowPersistenceProviderInfo(
    EngineeringWorkflowPersistenceProvider Provider,
    bool DurableEnabled,
    string ProviderLabel);
