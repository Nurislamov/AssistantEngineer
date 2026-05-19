using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;

namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class TestOnlyOwnershipBackfillApplyExecutor : IOwnershipBackfillApplyExecutor
{
    private static readonly HashSet<string> AllowedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "InMemory",
        "SQLiteTemp"
    };

    public Task<OwnershipBackfillApplyExecutionResult> ExecuteAsync(
        OwnershipBackfillApplyExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var findings = new List<OwnershipBackfillApplyExecutionFinding>();
        var previousValues = new List<OwnershipBackfillPreviousValueSnapshot>();

        if (!request.TestOnlyExecution)
        {
            findings.Add(NewFinding("TEST_ONLY_EXECUTION_REQUIRED", "Blocking", "Test-only execution flag must be true."));
            return Task.FromResult(FailedResult(request.Plan.PlannedRecords.Count, findings, previousValues));
        }

        if (!AllowedProviders.Contains(request.ExecutionProvider))
        {
            findings.Add(NewFinding("TEST_ONLY_PROVIDER_NOT_ALLOWED", "Blocking", "Execution provider must be InMemory or SQLiteTemp for test-only rehearsal."));
            return Task.FromResult(FailedResult(request.Plan.PlannedRecords.Count, findings, previousValues));
        }

        if (string.Equals(request.ExecutionProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.ExecutionProvider, "Production", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(NewFinding("TEST_ONLY_PRODUCTION_PROVIDER_REFUSED", "Blocking", "Production providers are not allowed for test-only rehearsal."));
            return Task.FromResult(FailedResult(request.Plan.PlannedRecords.Count, findings, previousValues));
        }

        if (!string.Equals(request.Plan.SummaryDraft.Mode, "PlanOnly", StringComparison.Ordinal))
        {
            findings.Add(NewFinding("TEST_ONLY_PLAN_MODE_INVALID", "Blocking", "Plan summary mode must be PlanOnly."));
            return Task.FromResult(FailedResult(request.Plan.PlannedRecords.Count, findings, previousValues));
        }

        if (!string.Equals(request.Plan.PlanHash, request.Signoff.PlanHash, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(NewFinding("TEST_ONLY_PLAN_SIGNOFF_HASH_MISMATCH", "Blocking", "Plan hash and signoff hash must match."));
            return Task.FromResult(FailedResult(request.Plan.PlannedRecords.Count, findings, previousValues));
        }

        if (request.Signoff.ExpiresAtUtc.HasValue && request.Signoff.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            findings.Add(NewFinding("TEST_ONLY_SIGNOFF_EXPIRED", "Blocking", "Plan signoff is expired for test-only rehearsal."));
            return Task.FromResult(FailedResult(request.Plan.PlannedRecords.Count, findings, previousValues));
        }

        var batchSize = request.Options.BatchSize > 0 ? request.Options.BatchSize : OwnershipBackfillConstants.DefaultBatchSize;
        var orderedRecords = request.Plan.PlannedRecords
            .OrderBy(record => record.RecordType, StringComparer.Ordinal)
            .ThenBy(record => record.RecordId, StringComparer.Ordinal)
            .ThenBy(record => record.DeterministicRecordHash, StringComparer.Ordinal)
            .ToArray();

        var totalUpdated = 0;
        var totalSkipped = 0;
        var totalFailed = 0;

        foreach (var batch in orderedRecords.Chunk(batchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var record in batch)
            {
                if (HasPayloadLikeContent(record))
                {
                    findings.Add(NewFinding("TEST_ONLY_FORBIDDEN_PLAN_CONTENT", "Error", "Plan record contains forbidden payload/secret-like content.", record.RecordType, record.RecordId));
                    totalFailed++;
                    continue;
                }

                if (record.Reason.Contains("Ambiguous", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(NewFinding("TEST_ONLY_AMBIGUOUS_RECORD_SKIPPED", "Warning", "Ambiguous planned record skipped.", record.RecordType, record.RecordId));
                    totalSkipped++;
                    continue;
                }

                if (!record.ProposedOrganizationId.HasValue || !record.ProposedProjectId.HasValue)
                {
                    findings.Add(NewFinding("TEST_ONLY_UNRESOLVED_RECORD_SKIPPED", "Warning", "Unresolved planned record skipped due to missing proposed ownership.", record.RecordType, record.RecordId));
                    totalSkipped++;
                    continue;
                }

                var expectedHash = ComputeRecordHash(
                    record.RecordType,
                    record.RecordId,
                    record.ProposedProjectId,
                    record.ProposedBuildingId,
                    record.ProposedOrganizationId,
                    record.ProposedOwnerUserId,
                    record.Reason);

                if (!string.Equals(expectedHash, record.DeterministicRecordHash, StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(NewFinding("TEST_ONLY_RECORD_HASH_MISMATCH", "Error", "Planned record hash mismatch.", record.RecordType, record.RecordId));
                    totalFailed++;
                    continue;
                }

                if (!request.TestRecordStore.TryGetRecord(record.RecordType, record.RecordId, out var current))
                {
                    findings.Add(NewFinding("TEST_ONLY_RECORD_NOT_FOUND", "Warning", "Target record is not present in test store.", record.RecordType, record.RecordId));
                    totalSkipped++;
                    continue;
                }

                if (current.SimulateFailure)
                {
                    findings.Add(NewFinding("TEST_ONLY_SIMULATED_FAILURE", "Error", "Record write-attempt failed in simulated test-only execution.", record.RecordType, record.RecordId));
                    totalFailed++;
                    continue;
                }

                if (HasConflict(current.ProjectId, record.CurrentProjectId, record.ProposedProjectId) ||
                    HasConflict(current.BuildingId, record.CurrentBuildingId, record.ProposedBuildingId) ||
                    HasConflict(current.OrganizationId, record.CurrentOrganizationId, record.ProposedOrganizationId) ||
                    HasConflict(current.OwnerUserId, record.CurrentOwnerUserId, record.ProposedOwnerUserId))
                {
                    findings.Add(NewFinding("TEST_ONLY_CURRENT_VALUE_CONFLICT", "Warning", "Current record values conflict with planned update.", record.RecordType, record.RecordId));
                    totalSkipped++;
                    continue;
                }

                var nextProjectId = record.ProposedProjectId ?? current.ProjectId;
                var nextBuildingId = record.ProposedBuildingId ?? current.BuildingId;
                var nextOrganizationId = record.ProposedOrganizationId ?? current.OrganizationId;
                var nextOwnerUserId = record.ProposedOwnerUserId ?? current.OwnerUserId;

                if (ValuesMatch(current.ProjectId, nextProjectId) &&
                    ValuesMatch(current.BuildingId, nextBuildingId) &&
                    ValuesMatch(current.OrganizationId, nextOrganizationId) &&
                    ValuesMatch(current.OwnerUserId, nextOwnerUserId))
                {
                    findings.Add(NewFinding("TEST_ONLY_ALREADY_MATCHES", "Info", "Current record already matches proposed ownership.", record.RecordType, record.RecordId));
                    totalSkipped++;
                    continue;
                }

                previousValues.Add(new OwnershipBackfillPreviousValueSnapshot
                {
                    RecordType = record.RecordType,
                    RecordId = record.RecordId,
                    PreviousProjectId = current.ProjectId,
                    PreviousBuildingId = current.BuildingId,
                    PreviousOrganizationId = current.OrganizationId,
                    PreviousOwnerUserId = current.OwnerUserId
                });

                request.TestRecordStore.UpsertRecord(new OwnershipBackfillTestRecordState
                {
                    RecordType = current.RecordType,
                    RecordId = current.RecordId,
                    ProjectId = nextProjectId,
                    BuildingId = nextBuildingId,
                    OrganizationId = nextOrganizationId,
                    OwnerUserId = nextOwnerUserId,
                    SimulateFailure = current.SimulateFailure
                });

                totalUpdated++;
            }
        }

        return Task.FromResult(new OwnershipBackfillApplyExecutionResult
        {
            Succeeded = totalFailed == 0,
            ExecutionId = BuildExecutionId(request.Plan.PlanHash),
            Mode = "TestOnlyRehearsal",
            TotalRecordsPlanned = request.Plan.PlannedRecords.Count,
            TotalRecordsUpdated = totalUpdated,
            TotalRecordsSkipped = totalSkipped,
            TotalRecordsFailed = totalFailed,
            Findings = findings,
            PreviousValues = previousValues,
            NonClaims = OwnershipBackfillConstants.NonClaims
        });
    }

    private static OwnershipBackfillApplyExecutionResult FailedResult(
        int totalRecordsPlanned,
        IReadOnlyList<OwnershipBackfillApplyExecutionFinding> findings,
        IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> previousValues)
    {
        return new OwnershipBackfillApplyExecutionResult
        {
            Succeeded = false,
            ExecutionId = BuildExecutionId("failed"),
            Mode = "TestOnlyRehearsal",
            TotalRecordsPlanned = totalRecordsPlanned,
            TotalRecordsUpdated = 0,
            TotalRecordsSkipped = 0,
            TotalRecordsFailed = 0,
            Findings = findings,
            PreviousValues = previousValues,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static bool HasPayloadLikeContent(OwnershipBackfillPlannedRecord record)
    {
        return record.Reason.Contains("payload", StringComparison.OrdinalIgnoreCase) ||
               record.SourceEvidence.Contains("payload", StringComparison.OrdinalIgnoreCase) ||
               record.SourceEvidence.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
               record.SourceEvidence.Contains("token", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasConflict(int? currentValue, int? expectedCurrent, int? proposedValue)
    {
        if (!currentValue.HasValue)
            return false;

        if (expectedCurrent.HasValue && currentValue.Value != expectedCurrent.Value)
            return true;

        return proposedValue.HasValue && currentValue.Value != proposedValue.Value;
    }

    private static bool ValuesMatch(int? left, int? right) => left == right;

    private static string ComputeRecordHash(
        string recordType,
        string recordId,
        int? proposedProjectId,
        int? proposedBuildingId,
        int? proposedOrganizationId,
        int? proposedOwnerUserId,
        string reason)
    {
        var canonical = string.Join(
            '|',
            recordType,
            recordId,
            ToToken(proposedProjectId),
            ToToken(proposedBuildingId),
            ToToken(proposedOrganizationId),
            ToToken(proposedOwnerUserId),
            reason);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildExecutionId(string token)
    {
        var now = DateTimeOffset.UtcNow;
        var safeToken = string.IsNullOrWhiteSpace(token) ? "execution" : token[..Math.Min(token.Length, 12)];
        return $"{now:yyyyMMddHHmmss}-{safeToken}";
    }

    private static string ToToken(int? value) =>
        value.HasValue
            ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "null";

    private static OwnershipBackfillApplyExecutionFinding NewFinding(
        string code,
        string severity,
        string message,
        string? recordType = null,
        string? recordId = null)
    {
        return new OwnershipBackfillApplyExecutionFinding
        {
            Code = code,
            Severity = severity,
            Message = message,
            RecordType = recordType,
            RecordId = recordId
        };
    }
}
