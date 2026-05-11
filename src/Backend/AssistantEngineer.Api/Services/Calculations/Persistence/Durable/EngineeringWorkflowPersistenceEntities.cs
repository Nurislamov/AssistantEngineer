namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EngineeringProjectEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<EngineeringWorkflowStateEntity> WorkflowStates { get; set; } = [];

    public List<EngineeringCalculationScenarioEntity> Scenarios { get; set; } = [];

    public List<EngineeringScenarioHistoryEntryEntity> HistoryEntries { get; set; } = [];
}

public sealed class EngineeringWorkflowStateEntity
{
    public string Id { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public int? BuildingId { get; set; }

    public int Version { get; set; }

    public string CurrentStep { get; set; } = string.Empty;

    public string StateJson { get; set; } = string.Empty;

    public string? ValidationDiagnosticsJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public EngineeringProjectEntity? Project { get; set; }
}

public sealed class EngineeringCalculationScenarioEntity
{
    public string Id { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public int? BuildingId { get; set; }

    public string ScenarioKind { get; set; } = string.Empty;

    public string ExecutionMode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string RequestJson { get; set; } = string.Empty;

    public string? ResultSummaryJson { get; set; }

    public string? DiagnosticsJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public double? DurationMs { get; set; }

    public EngineeringProjectEntity? Project { get; set; }

    public List<EngineeringCalculationArtifactEntity> Artifacts { get; set; } = [];

    public List<EngineeringScenarioHistoryEntryEntity> HistoryEntries { get; set; } = [];

    public List<EngineeringCalculationJobEntity> Jobs { get; set; } = [];
}

public sealed class EngineeringCalculationArtifactEntity
{
    public string Id { get; set; } = string.Empty;

    public string ScenarioId { get; set; } = string.Empty;

    public string ArtifactKind { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int? SizeBytes { get; set; }

    public string? Checksum { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public EngineeringCalculationScenarioEntity? Scenario { get; set; }
}

public sealed class EngineeringScenarioHistoryEntryEntity
{
    public string Id { get; set; } = string.Empty;

    public string ScenarioId { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public string EventKind { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? DiagnosticsJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public EngineeringCalculationScenarioEntity? Scenario { get; set; }

    public EngineeringProjectEntity? Project { get; set; }
}

public sealed class EngineeringCalculationJobEntity
{
    public string Id { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public string ScenarioId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ExecutionMode { get; set; } = string.Empty;

    public string RequestJson { get; set; } = string.Empty;

    public string? ResultSummaryJson { get; set; }

    public string? DiagnosticsJson { get; set; }

    public int ProgressPercent { get; set; }

    public string CurrentStep { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? QueuedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public double? DurationMs { get; set; }

    public int RetryCount { get; set; }

    public bool CancellationRequested { get; set; }

    public string? ClaimedByWorkerId { get; set; }

    public DateTimeOffset? ClaimedAtUtc { get; set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }

    public EngineeringProjectEntity? Project { get; set; }

    public EngineeringCalculationScenarioEntity? Scenario { get; set; }

    public List<EngineeringCalculationJobEventEntity> Events { get; set; } = [];
}

public sealed class EngineeringCalculationJobEventEntity
{
    public string Id { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;

    public string ScenarioId { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string EventKind { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? DiagnosticsJson { get; set; }

    public int? ProgressPercent { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public EngineeringCalculationJobEntity? Job { get; set; }
}

public sealed class EngineeringWorkflowIdempotencyRecordEntity
{
    public long Id { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string RequestFingerprint { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ResponseJson { get; set; }

    public string? ResponseReferenceId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? FailureReason { get; set; }
}
