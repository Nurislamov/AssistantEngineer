namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency;

public interface IEngineeringIdempotencyService
{
    Task<EngineeringIdempotencyEvaluationResult> EvaluateAsync(
        string? idempotencyKey,
        string scope,
        string requestFingerprint,
        CancellationToken cancellationToken);

    Task RecordSuccessAsync(
        string idempotencyKey,
        string scope,
        string requestFingerprint,
        string? responseJson,
        string? responseReferenceId,
        CancellationToken cancellationToken);
}
