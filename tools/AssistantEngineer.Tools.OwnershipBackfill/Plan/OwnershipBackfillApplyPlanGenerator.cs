using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillApplyPlanGenerator : IOwnershipBackfillApplyPlanGenerator
{
    private const string Stage = "P6-05";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> ResolvableUnresolvedReasons =
    [
        OwnershipBackfillUnresolvedReasons.ProjectOrganizationMissing,
        OwnershipBackfillUnresolvedReasons.BuildingProjectOrganizationMissing,
        OwnershipBackfillUnresolvedReasons.WorkflowStateOwnershipMetadataMissing,
        OwnershipBackfillUnresolvedReasons.ScenarioOwnershipMetadataMissing,
        OwnershipBackfillUnresolvedReasons.JobOwnershipMetadataMissing,
        OwnershipBackfillUnresolvedReasons.JobEventOwnershipMetadataMissing,
        OwnershipBackfillUnresolvedReasons.ScenarioHistoryOwnershipMetadataMissing
    ];

    private static readonly HashSet<string> AmbiguousReasons =
    [
        OwnershipBackfillUnresolvedReasons.WorkflowStateOwnershipAmbiguous,
        OwnershipBackfillUnresolvedReasons.ScenarioOwnershipAmbiguous,
        OwnershipBackfillUnresolvedReasons.JobOwnershipAmbiguous,
        OwnershipBackfillUnresolvedReasons.JobEventOwnershipAmbiguous,
        OwnershipBackfillUnresolvedReasons.ScenarioHistoryOwnershipAmbiguous,
        "AmbiguousOwnership"
    ];

    private static readonly HashSet<string> ForbiddenPlanPropertyFragments =
    [
        "payload",
        "secret",
        "token",
        "apiKey",
        "json"
    ];

    public async Task<OwnershipBackfillPlanResult> GenerateAsync(
        OwnershipBackfillPlanOptions options,
        CancellationToken cancellationToken = default)
    {
        var evidenceDirectory = ValidateAndGetEvidenceDirectory(options.EvidenceDirectory);
        var gateResultPath = ValidateAndGetFilePath(options.GateResultPath, "gate result");

        var gateResult = await ReadJsonAsync<OwnershipBackfillGateResult>(gateResultPath, cancellationToken);
        if (!gateResult.Passed)
            throw new OwnershipBackfillPlanGateFailedException("Gate result is failed. plan-apply requires Passed=true.");

        if (gateResult.NonClaims.Count == 0)
            throw new InvalidOperationException("Gate result non-claims are required for plan generation.");

        var summaryPath = ResolveLatestSummaryPath(evidenceDirectory);
        var summary = await ReadJsonAsync<OwnershipBackfillDryRunSummary>(summaryPath, cancellationToken);

        if (!string.Equals(summary.Mode, OwnershipBackfillRunMode.DryRun.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("Dry-run summary Mode must equal DryRun.");

        if (summary.NonClaims.Count == 0)
            throw new InvalidOperationException("Dry-run summary non-claims are required for plan generation.");

        var unresolvedPath = CombineSafe(evidenceDirectory, $"ownership-backfill-unresolved-records-{SanitizeFileToken(summary.RunId)}.json");
        var previousPath = CombineSafe(evidenceDirectory, $"ownership-backfill-previous-values-{SanitizeFileToken(summary.RunId)}.json");

        if (!File.Exists(previousPath))
            throw new InvalidOperationException("Previous-values evidence file is required for plan generation.");

        List<OwnershipBackfillUnresolvedRecord> unresolvedRecords;
        HashSet<string> unresolvedPropertyNames;

        if (File.Exists(unresolvedPath))
        {
            unresolvedRecords = await ReadJsonListAsync<OwnershipBackfillUnresolvedRecord>(unresolvedPath, cancellationToken);
            unresolvedPropertyNames = await ReadArrayPropertyNamesAsync(unresolvedPath, cancellationToken);
        }
        else if (summary.TotalRecordsUnresolved == 0)
        {
            unresolvedRecords = [];
            unresolvedPropertyNames = [];
        }
        else
        {
            throw new InvalidOperationException("Unresolved records evidence file is required when TotalRecordsUnresolved is greater than zero.");
        }

        EnsureNoForbiddenPropertyNames(unresolvedPropertyNames);

        var previousValues = await ReadJsonListAsync<OwnershipBackfillPreviousValueSnapshot>(previousPath, cancellationToken);
        var previousByKey = previousValues
            .GroupBy(item => BuildRecordKey(item.RecordType, item.RecordId), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var skippedByReason = new Dictionary<string, int>(StringComparer.Ordinal);
        var findings = new List<OwnershipBackfillPlanFinding>();
        var plannedRecords = BuildPlannedRecords(
            unresolvedRecords,
            previousByKey,
            summary.RunId,
            skippedByReason,
            findings);

        if (options.MaxPlannedRecords.HasValue && plannedRecords.Count > options.MaxPlannedRecords.Value)
        {
            var originalCount = plannedRecords.Count;
            plannedRecords = plannedRecords
                .Take(options.MaxPlannedRecords.Value)
                .ToList();

            AddSkipped(skippedByReason, "TrimmedByMaxPlannedRecords", originalCount - options.MaxPlannedRecords.Value);
            findings.Add(new OwnershipBackfillPlanFinding
            {
                Code = "PLAN_MAX_RECORDS_TRIMMED",
                Severity = "Warning",
                Message = "Plan was trimmed by --max-planned-records threshold.",
                RecordType = null,
                RecordId = null
            });
        }

        plannedRecords = plannedRecords
            .OrderBy(record => record.RecordType, StringComparer.Ordinal)
            .ThenBy(record => record.RecordId, StringComparer.Ordinal)
            .ThenBy(record => record.DeterministicRecordHash, StringComparer.Ordinal)
            .ToList();

        var planHash = ComputePlanHash(summary, gateResult, plannedRecords, options.RulesetVersion);
        var planId = planHash[..16];
        var runId = BuildRunId();

        var plannedByRecordType = plannedRecords
            .GroupBy(record => record.RecordType, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var summaryDraft = new OwnershipBackfillApplySummaryDraft
        {
            PlanId = planId,
            PlanHash = planHash,
            Mode = "PlanOnly",
            TotalRecordsPlanned = plannedRecords.Count,
            TotalRecordsSkipped = skippedByReason.Values.Sum(),
            TotalRecordsUnresolved = summary.TotalRecordsUnresolved,
            PlannedByRecordType = plannedByRecordType,
            SkippedByReason = skippedByReason,
            RequiredFutureApplyPreconditions =
            [
                "validate-evidence gate result Passed=true",
                "apply mode must be explicitly enabled in future stage",
                "confirmation phrase must match exactly",
                "previous-values snapshot must be written before any future write batch",
                "plan hash must match approved evidence inputs"
            ],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        return new OwnershipBackfillPlanResult
        {
            Succeeded = true,
            RunId = runId,
            PlanId = planId,
            PlanHash = planHash,
            RulesetVersion = options.RulesetVersion,
            PlannedRecords = plannedRecords,
            SummaryDraft = summaryDraft,
            Findings = findings,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static List<OwnershipBackfillPlannedRecord> BuildPlannedRecords(
        IReadOnlyList<OwnershipBackfillUnresolvedRecord> unresolvedRecords,
        IReadOnlyDictionary<string, OwnershipBackfillPreviousValueSnapshot> previousByKey,
        string summaryRunId,
        IDictionary<string, int> skippedByReason,
        IList<OwnershipBackfillPlanFinding> findings)
    {
        var plannedRecords = new List<OwnershipBackfillPlannedRecord>();

        foreach (var unresolvedRecord in unresolvedRecords
                     .OrderBy(record => record.RecordType, StringComparer.Ordinal)
                     .ThenBy(record => record.RecordId, StringComparer.Ordinal))
        {
            var key = BuildRecordKey(unresolvedRecord.RecordType, unresolvedRecord.RecordId);
            previousByKey.TryGetValue(key, out var previous);

            if (AmbiguousReasons.Contains(unresolvedRecord.Reason))
            {
                AddSkipped(skippedByReason, "AmbiguousRecord");
                continue;
            }

            if (!ResolvableUnresolvedReasons.Contains(unresolvedRecord.Reason))
            {
                AddSkipped(skippedByReason, "NonResolvableReason");
                continue;
            }

            if (!unresolvedRecord.CandidateOrganizationId.HasValue)
            {
                AddSkipped(skippedByReason, "MissingCandidateOrganization");
                findings.Add(new OwnershipBackfillPlanFinding
                {
                    Code = "PLAN_CANDIDATE_ORGANIZATION_MISSING",
                    Severity = "Warning",
                    Message = "Record skipped because candidate organization is missing in evidence.",
                    RecordType = unresolvedRecord.RecordType,
                    RecordId = unresolvedRecord.RecordId
                });
                continue;
            }

            if (!unresolvedRecord.CandidateProjectId.HasValue)
            {
                AddSkipped(skippedByReason, "MissingCandidateProject");
                findings.Add(new OwnershipBackfillPlanFinding
                {
                    Code = "PLAN_CANDIDATE_PROJECT_MISSING",
                    Severity = "Warning",
                    Message = "Record skipped because candidate project is missing in evidence.",
                    RecordType = unresolvedRecord.RecordType,
                    RecordId = unresolvedRecord.RecordId
                });
                continue;
            }

            var currentProjectId = previous?.PreviousProjectId;
            var currentBuildingId = previous?.PreviousBuildingId;
            var currentOrganizationId = previous?.PreviousOrganizationId;
            var currentOwnerUserId = previous?.PreviousOwnerUserId;

            var proposedProjectId = unresolvedRecord.CandidateProjectId;
            var proposedBuildingId = unresolvedRecord.CandidateBuildingId ?? currentBuildingId;
            var proposedOrganizationId = unresolvedRecord.CandidateOrganizationId;
            var proposedOwnerUserId = currentOwnerUserId;

            if (IsValueConflict(currentProjectId, proposedProjectId) ||
                IsValueConflict(currentBuildingId, proposedBuildingId) ||
                IsValueConflict(currentOrganizationId, proposedOrganizationId))
            {
                AddSkipped(skippedByReason, "CurrentValueConflict");
                continue;
            }

            if (ValuesMatch(currentProjectId, proposedProjectId) &&
                ValuesMatch(currentBuildingId, proposedBuildingId) &&
                ValuesMatch(currentOrganizationId, proposedOrganizationId) &&
                ValuesMatch(currentOwnerUserId, proposedOwnerUserId))
            {
                AddSkipped(skippedByReason, "AlreadyMatches");
                continue;
            }

            var recordHash = ComputeRecordHash(
                unresolvedRecord.RecordType,
                unresolvedRecord.RecordId,
                proposedProjectId,
                proposedBuildingId,
                proposedOrganizationId,
                proposedOwnerUserId,
                unresolvedRecord.Reason);

            plannedRecords.Add(new OwnershipBackfillPlannedRecord
            {
                RecordType = unresolvedRecord.RecordType,
                RecordId = unresolvedRecord.RecordId,
                CurrentProjectId = currentProjectId,
                CurrentBuildingId = currentBuildingId,
                CurrentOrganizationId = currentOrganizationId,
                CurrentOwnerUserId = currentOwnerUserId,
                ProposedProjectId = proposedProjectId,
                ProposedBuildingId = proposedBuildingId,
                ProposedOrganizationId = proposedOrganizationId,
                ProposedOwnerUserId = proposedOwnerUserId,
                Reason = unresolvedRecord.Reason,
                SourceEvidence = $"ownership-backfill-unresolved-records-{SanitizeFileToken(summaryRunId)}.json",
                DeterministicRecordHash = recordHash
            });
        }

        return plannedRecords;
    }

    private static string ComputePlanHash(
        OwnershipBackfillDryRunSummary summary,
        OwnershipBackfillGateResult gateResult,
        IReadOnlyList<OwnershipBackfillPlannedRecord> plannedRecords,
        string rulesetVersion)
    {
        var canonicalBuilder = new StringBuilder();
        canonicalBuilder.Append("stage=").Append(Stage).Append(';');
        canonicalBuilder.Append("ruleset=").Append(rulesetVersion).Append(';');

        canonicalBuilder.Append("summary:");
        canonicalBuilder.Append(summary.RunId).Append('|');
        canonicalBuilder.Append(summary.Mode).Append('|');
        canonicalBuilder.Append(summary.TotalRecordsScanned).Append('|');
        canonicalBuilder.Append(summary.TotalRecordsResolvable).Append('|');
        canonicalBuilder.Append(summary.TotalRecordsUnresolved).Append('|');

        foreach (var reason in summary.UnresolvedByReason.OrderBy(item => item.Key, StringComparer.Ordinal))
            canonicalBuilder.Append(reason.Key).Append('=').Append(reason.Value).Append(';');

        foreach (var metric in summary.RecordTypeMetrics
                     .OrderBy(item => item.RecordType, StringComparer.Ordinal))
        {
            canonicalBuilder.Append(metric.RecordType).Append('|')
                .Append(metric.TotalRecords).Append('|')
                .Append(metric.ResolvableRecords).Append('|')
                .Append(metric.UnresolvedRecords).Append('|')
                .Append(metric.AmbiguousRecords).Append(';');

            foreach (var unresolvedReason in metric.UnresolvedByReason.OrderBy(item => item.Key, StringComparer.Ordinal))
                canonicalBuilder.Append(unresolvedReason.Key).Append('=').Append(unresolvedReason.Value).Append(';');
        }

        canonicalBuilder.Append("gate:");
        canonicalBuilder.Append(gateResult.Passed).Append('|');
        canonicalBuilder.Append(gateResult.Summary).Append('|');
        foreach (var threshold in gateResult.Thresholds.OrderBy(item => item.Key, StringComparer.Ordinal))
            canonicalBuilder.Append(threshold.Key).Append('=').Append(threshold.Value).Append(';');

        foreach (var finding in gateResult.Findings
                     .OrderBy(item => item.Code, StringComparer.Ordinal)
                     .ThenBy(item => item.Severity, StringComparer.Ordinal)
                     .ThenBy(item => item.RecordType, StringComparer.Ordinal)
                     .ThenBy(item => item.Metric, StringComparer.Ordinal))
        {
            canonicalBuilder.Append(finding.Code).Append('|')
                .Append(finding.Severity).Append('|')
                .Append(finding.RecordType).Append('|')
                .Append(finding.Metric).Append('|')
                .Append(finding.Expected).Append('|')
                .Append(finding.Actual).Append(';');
        }

        canonicalBuilder.Append("plan:");
        foreach (var record in plannedRecords
                     .OrderBy(item => item.RecordType, StringComparer.Ordinal)
                     .ThenBy(item => item.RecordId, StringComparer.Ordinal)
                     .ThenBy(item => item.DeterministicRecordHash, StringComparer.Ordinal))
        {
            canonicalBuilder.Append(record.RecordType).Append('|')
                .Append(record.RecordId).Append('|')
                .Append(ToToken(record.ProposedProjectId)).Append('|')
                .Append(ToToken(record.ProposedBuildingId)).Append('|')
                .Append(ToToken(record.ProposedOrganizationId)).Append('|')
                .Append(ToToken(record.ProposedOwnerUserId)).Append('|')
                .Append(record.Reason).Append('|')
                .Append(record.DeterministicRecordHash).Append(';');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalBuilder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

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

    private static bool IsValueConflict(int? currentValue, int? proposedValue)
    {
        return currentValue.HasValue && proposedValue.HasValue && currentValue.Value != proposedValue.Value;
    }

    private static bool ValuesMatch(int? currentValue, int? proposedValue)
    {
        return currentValue == proposedValue;
    }

    private static string BuildRecordKey(string recordType, string recordId)
    {
        return $"{recordType}:{recordId}";
    }

    private static async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var model = JsonSerializer.Deserialize<T>(content, JsonOptions);

        if (model is null)
            throw new InvalidOperationException($"Failed to parse JSON file: {Path.GetFileName(path)}");

        return model;
    }

    private static async Task<List<T>> ReadJsonListAsync<T>(string path, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var model = JsonSerializer.Deserialize<List<T>>(content, JsonOptions);
        return model ?? [];
    }

    private static async Task<HashSet<string>> ReadArrayPropertyNamesAsync(string path, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        using var document = JsonDocument.Parse(content);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Expected array JSON in file: {Path.GetFileName(path)}");

        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var property in item.EnumerateObject())
                propertyNames.Add(property.Name);
        }

        return propertyNames;
    }

    private static void EnsureNoForbiddenPropertyNames(IReadOnlySet<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (ForbiddenPlanPropertyFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Unresolved evidence contains forbidden payload or secret-like fields.");
            }
        }
    }

    private static void AddSkipped(IDictionary<string, int> skippedByReason, string reason, int increment = 1)
    {
        skippedByReason[reason] = skippedByReason.TryGetValue(reason, out var existing)
            ? existing + increment
            : increment;
    }

    private static string ValidateAndGetEvidenceDirectory(string? evidenceDirectory)
    {
        if (string.IsNullOrWhiteSpace(evidenceDirectory))
            throw new InvalidOperationException("--evidence is required for plan-apply.");

        var fullPath = Path.GetFullPath(evidenceDirectory);
        if (!Directory.Exists(fullPath))
            throw new InvalidOperationException("Evidence directory does not exist.");

        return fullPath;
    }

    private static string ValidateAndGetFilePath(string? filePath, string label)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new InvalidOperationException($"{label} path is required.");

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"{label} file does not exist.");

        return fullPath;
    }

    private static string ResolveLatestSummaryPath(string evidenceDirectory)
    {
        var summaries = Directory.GetFiles(evidenceDirectory, "ownership-backfill-dry-run-summary-*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        if (summaries.Length == 0)
            throw new InvalidOperationException("No dry-run summary JSON was found in evidence directory.");

        return summaries[0];
    }

    private static string CombineSafe(string outputDirectory, string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/']) >= 0)
            throw new InvalidOperationException("File name must not contain path separators.");

        var outputRoot = EnsureTrailingSeparator(Path.GetFullPath(outputDirectory));
        var candidate = Path.GetFullPath(Path.Combine(outputDirectory, fileName));

        if (!candidate.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal outside directory is not allowed.");

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private static string SanitizeFileToken(string token)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(token.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "run" : sanitized;
    }

    private static string BuildRunId()
    {
        var now = DateTimeOffset.UtcNow;
        return $"{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..23];
    }

    private static string ToToken(int? value)
    {
        return value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null";
    }
}
