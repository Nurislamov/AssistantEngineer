namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency;

public enum EngineeringIdempotencyEvaluationKind
{
    Bypass,
    Proceed,
    Replay,
    Conflict
}

public sealed record EngineeringIdempotencyReplayPayload(
    string? ResponseJson,
    string? ResponseReferenceId);

public sealed record EngineeringIdempotencyEvaluationResult(
    EngineeringIdempotencyEvaluationKind Kind,
    EngineeringIdempotencyReplayPayload? ReplayPayload = null,
    string? ConflictCode = null,
    string? ConflictMessage = null);
