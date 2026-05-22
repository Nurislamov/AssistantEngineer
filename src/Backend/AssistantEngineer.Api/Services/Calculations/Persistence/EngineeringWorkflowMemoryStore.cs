using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using System.Collections.Concurrent;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowMemoryStore
{
    public ConcurrentDictionary<int, EngineeringProjectRecordDto> Projects { get; } = new();

    public ConcurrentDictionary<string, EngineeringWorkflowStateRecordDto> WorkflowStatesById { get; } = new(StringComparer.Ordinal);

    public ConcurrentDictionary<string, EngineeringCalculationScenarioRecordDto> ScenariosById { get; } = new(StringComparer.Ordinal);

    public ConcurrentDictionary<string, EngineeringCalculationArtifactRecordDto> ArtifactsById { get; } = new(StringComparer.Ordinal);

    public ConcurrentDictionary<string, EngineeringScenarioHistoryEntryDto> HistoryById { get; } = new(StringComparer.Ordinal);

    public ConcurrentDictionary<string, EngineeringCalculationJobRecordDto> JobsById { get; } = new(StringComparer.Ordinal);

    public ConcurrentDictionary<string, EngineeringCalculationJobEventRecordDto> JobEventsById { get; } = new(StringComparer.Ordinal);
}
