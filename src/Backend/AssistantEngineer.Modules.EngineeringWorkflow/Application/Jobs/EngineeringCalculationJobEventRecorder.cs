using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Jobs;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Jobs;

public sealed class EngineeringCalculationJobEventRecorder
{
    private readonly IEngineeringCalculationJobEventRepository _jobEventRepository;
    private readonly EngineeringCalculationJobPayloadCodec _payloadCodec;

    public EngineeringCalculationJobEventRecorder(
        IEngineeringCalculationJobEventRepository jobEventRepository,
        EngineeringCalculationJobPayloadCodec payloadCodec)
    {
        _jobEventRepository = jobEventRepository;
        _payloadCodec = payloadCodec;
    }

    public async Task AppendAsync(
        EngineeringCalculationJobRecordDto job,
        EngineeringCalculationJobStatus status,
        string message,
        string? moduleKind,
        int? progressPercent,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var eventId = $"{job.JobId}:{status}:{timestamp.ToUnixTimeMilliseconds()}";
        var eventRecord = new EngineeringCalculationJobEventRecordDto(
            EventId: eventId,
            JobId: job.JobId,
            ScenarioId: job.ScenarioId,
            ProjectId: job.ProjectId,
            Status: status,
            EventKind: status.ToString(),
            Message: moduleKind is null ? message : $"{message} ({moduleKind})",
            DiagnosticsJson: _payloadCodec.Serialize(_payloadCodec.SortAndDistinctDiagnostics(diagnostics)),
            ProgressPercent: progressPercent,
            CreatedAtUtc: timestamp);

        await _jobEventRepository.AppendAsync(eventRecord, cancellationToken);
    }

    public EngineeringCalculationJobEventDto MapToDto(EngineeringCalculationJobEventRecordDto source)
    {
        return new EngineeringCalculationJobEventDto(
            EventId: source.EventId,
            JobId: source.JobId,
            ScenarioId: source.ScenarioId,
            Status: source.Status,
            Message: source.Message,
            ModuleKind: null,
            ProgressPercent: source.ProgressPercent,
            Diagnostics: _payloadCodec.DeserializeDiagnostics(source.DiagnosticsJson),
            CreatedAtUtc: source.CreatedAtUtc);
    }
}
