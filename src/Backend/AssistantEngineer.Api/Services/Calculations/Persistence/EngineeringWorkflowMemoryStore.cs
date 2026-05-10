using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowMemoryStore
{
    public object SyncRoot { get; } = new();

    public Dictionary<int, EngineeringProjectRecordDto> Projects { get; } = [];

    public Dictionary<string, EngineeringWorkflowStateRecordDto> WorkflowStatesById { get; } = [];

    public Dictionary<int, List<string>> WorkflowStateIdsByProjectId { get; } = [];

    public Dictionary<string, EngineeringCalculationScenarioRecordDto> ScenariosById { get; } = [];

    public Dictionary<int, List<string>> ScenarioIdsByProjectId { get; } = [];

    public Dictionary<string, EngineeringCalculationArtifactRecordDto> ArtifactsById { get; } = [];

    public Dictionary<string, List<string>> ArtifactIdsByScenarioId { get; } = [];

    public Dictionary<string, EngineeringScenarioHistoryEntryDto> HistoryById { get; } = [];

    public Dictionary<string, List<string>> HistoryIdsByScenarioId { get; } = [];

    public Dictionary<int, List<string>> HistoryIdsByProjectId { get; } = [];

    public Dictionary<string, EngineeringCalculationJobRecordDto> JobsById { get; } = [];

    public Dictionary<int, List<string>> JobIdsByProjectId { get; } = [];

    public Dictionary<string, EngineeringCalculationJobEventRecordDto> JobEventsById { get; } = [];

    public Dictionary<string, List<string>> JobEventIdsByJobId { get; } = [];
}
