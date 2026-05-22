namespace AssistantEngineer.Api.Security.Authorization;

public enum ProtectedEndpointScopeKind
{
    None = 0,
    Project = 1,
    Building = 2,
    Workflow = 3,
    WorkflowScenario = 4,
    WorkflowJob = 5,
    Floor = 6,
    Room = 7
}
