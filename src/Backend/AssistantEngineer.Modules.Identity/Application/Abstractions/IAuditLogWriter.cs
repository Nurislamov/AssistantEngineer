using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Identity.Application.Abstractions;

public interface IAuditLogWriter
{
    Task<Result<AuditEventRecord>> WriteAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AuditEventRecord>>> QueryByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AuditEventRecord>>> QueryByResourceAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);
}
