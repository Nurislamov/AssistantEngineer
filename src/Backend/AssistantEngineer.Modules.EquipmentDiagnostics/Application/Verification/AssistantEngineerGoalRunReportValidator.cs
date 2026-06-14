namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public sealed class AssistantEngineerGoalRunReportValidator
{
    private static readonly string[] AllowedStatuses = ["pass", "fail", "not_run"];

    private static readonly string[] ForbiddenClaims =
    [
        string.Concat("production ", "ready"),
        string.Concat("public release ", "ready"),
        string.Concat("fully autonomous ", "engineer"),
        string.Concat("autonomous production ", "execution"),
        string.Concat("AI ", "diagnosis"),
        string.Concat("RAG ", "diagnosis"),
        string.Concat("vector search ", "diagnosis"),
        string.Concat("full vendor manual ", "coverage"),
        string.Concat("full ", "parity"),
        string.Concat("ManualVerified ", "promotion")
    ];

    private static readonly string[] ForbiddenArtifactFragments =
    [
        ".pdf",
        ".log",
        "log-dump",
        "log_dump",
        "manual",
        "secret",
        "bottoken",
        "webhooksecret"
    ];

    public AssistantEngineerGoalRunValidationResult Validate(AssistantEngineerGoalRunReport? report)
    {
        var blockers = new List<string>();
        var warnings = new List<string>();
        if (report is null)
        {
            blockers.Add("Goal-run report is required.");
            return Result(warnings, blockers);
        }

        Require(report.GoalId, "goalId", blockers);
        Require(report.Title, "title", blockers);
        Require(report.SourceBranch, "sourceBranch", blockers);
        Require(report.TargetBranch, "targetBranch", blockers);
        RequireItems(report.Scope, "scope", blockers);
        RequireItems(report.Constraints, "constraints", blockers);
        ValidatePreflight(report.Preflight, warnings, blockers);
        ValidatePhases(report.Phases, warnings, blockers);
        ValidateFinalAudit(report.FinalAudit, warnings, blockers);

        foreach (var blocker in report.Blockers ?? [])
        {
            if (!string.IsNullOrWhiteSpace(blocker))
            {
                blockers.Add($"Reported blocker: {blocker.Trim()}");
            }
        }

        foreach (var warning in report.Warnings ?? [])
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                warnings.Add($"Reported warning: {warning.Trim()}");
            }
        }

        ValidateGeneratedArtifacts(report.GeneratedArtifacts, blockers);
        ValidateForbiddenClaims(report, blockers);
        return Result(warnings, blockers);
    }

    private static void ValidatePreflight(
        AssistantEngineerGoalRunPreflight? preflight,
        ICollection<string> warnings,
        ICollection<string> blockers)
    {
        if (preflight is null)
        {
            blockers.Add("preflight is required.");
            return;
        }

        if (preflight.Commands is null || preflight.Commands.Count == 0)
        {
            blockers.Add("preflight.commands must contain at least one command.");
            return;
        }

        foreach (var command in preflight.Commands)
        {
            Require(command.Name, "preflight command name", blockers);
            Require(command.Command, "preflight command", blockers);
            ValidateStatus(command.Status, "preflight command", warnings, blockers);
            if (IsStatus(command.Status, "pass"))
            {
                Require(command.Evidence, "passed preflight command evidence", blockers);
            }

            if (IsStatus(command.Status, "fail"))
            {
                blockers.Add($"Preflight command failed: {command.Name ?? "<unnamed>"}.");
            }
        }
    }

    private static void ValidatePhases(
        IReadOnlyList<AssistantEngineerGoalRunPhase>? phases,
        ICollection<string> warnings,
        ICollection<string> blockers)
    {
        if (phases is null || phases.Count == 0)
        {
            blockers.Add("phases must contain at least one phase.");
            return;
        }

        foreach (var phase in phases)
        {
            if (phase.Number <= 0)
            {
                blockers.Add("Phase numbers must be positive.");
            }

            Require(phase.Title, $"phase {phase.Number} title", blockers);
            ValidateStatus(phase.Status, $"phase {phase.Number}", warnings, blockers);
            RequireItems(phase.Deliverables, $"phase {phase.Number} deliverables", blockers);
            RequireItems(phase.AcceptanceCriteria, $"phase {phase.Number} acceptanceCriteria", blockers);
            RequireItems(phase.MandatoryCommands, $"phase {phase.Number} mandatoryCommands", blockers);
            if (IsStatus(phase.Status, "pass"))
            {
                RequireItems(phase.Evidence, $"passed phase {phase.Number} evidence", blockers);
            }

            if (IsStatus(phase.Status, "fail"))
            {
                blockers.Add($"Phase {phase.Number} failed.");
            }
        }
    }

    private static void ValidateFinalAudit(
        AssistantEngineerGoalRunFinalAudit? audit,
        ICollection<string> warnings,
        ICollection<string> blockers)
    {
        if (audit is null)
        {
            blockers.Add("finalAudit is required.");
            return;
        }

        ValidateStatus(audit.Status, "finalAudit", warnings, blockers);
        Require(audit.RoadmapCoverage, "finalAudit.roadmapCoverage", blockers);
        Require(audit.PhaseCompletion, "finalAudit.phaseCompletion", blockers);
        Require(audit.ChangedFilesReview, "finalAudit.changedFilesReview", blockers);
        Require(audit.ForbiddenFilesReview, "finalAudit.forbiddenFilesReview", blockers);
        Require(audit.ForbiddenClaimsReview, "finalAudit.forbiddenClaimsReview", blockers);
        Require(audit.SecretsScan, "finalAudit.secretsScan", blockers);
        Require(audit.GeneratedArtifacts, "finalAudit.generatedArtifacts", blockers);
        Require(audit.MergeReadiness, "finalAudit.mergeReadiness", blockers);
        if (IsStatus(audit.Status, "fail"))
        {
            blockers.Add("Final audit failed.");
        }
    }

    private static void ValidateGeneratedArtifacts(
        IReadOnlyList<string>? generatedArtifacts,
        ICollection<string> blockers)
    {
        foreach (var rawPath in generatedArtifacts ?? [])
        {
            var path = rawPath.Replace('\\', '/').Trim();
            if (path.StartsWith("docs/", StringComparison.OrdinalIgnoreCase))
            {
                blockers.Add($"Generated artifact must not be written under docs/: {path}.");
            }

            var forbidden = ForbiddenArtifactFragments.FirstOrDefault(fragment =>
                path.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            if (forbidden is not null)
            {
                blockers.Add($"Generated artifact path contains forbidden fragment '{forbidden}': {path}.");
            }
        }
    }

    private static void ValidateForbiddenClaims(
        AssistantEngineerGoalRunReport report,
        ICollection<string> blockers)
    {
        var text = EnumerateText(report);
        foreach (var claim in ForbiddenClaims)
        {
            if (text.Any(value => value.Contains(claim, StringComparison.OrdinalIgnoreCase)))
            {
                blockers.Add($"Goal-run report contains forbidden claim '{claim}'.");
            }
        }
    }

    private static IReadOnlyList<string> EnumerateText(AssistantEngineerGoalRunReport report)
    {
        var values = new List<string?>
        {
            report.GoalId, report.Title, report.SourceBranch, report.TargetBranch,
            report.FinalAudit?.Status, report.FinalAudit?.RoadmapCoverage, report.FinalAudit?.PhaseCompletion,
            report.FinalAudit?.ChangedFilesReview, report.FinalAudit?.ForbiddenFilesReview,
            report.FinalAudit?.ForbiddenClaimsReview, report.FinalAudit?.SecretsScan,
            report.FinalAudit?.GeneratedArtifacts, report.FinalAudit?.MergeReadiness
        };
        values.AddRange(report.Scope ?? []);
        values.AddRange(report.OutOfScope ?? []);
        values.AddRange(report.Constraints ?? []);
        values.AddRange(report.Warnings ?? []);
        values.AddRange(report.Blockers ?? []);
        values.AddRange(report.GeneratedArtifacts ?? []);
        foreach (var command in report.Preflight?.Commands ?? [])
        {
            values.AddRange([command.Name, command.Command, command.Status, command.Evidence]);
        }

        foreach (var phase in report.Phases ?? [])
        {
            values.AddRange([phase.Title, phase.Status]);
            values.AddRange(phase.Deliverables ?? []);
            values.AddRange(phase.AcceptanceCriteria ?? []);
            values.AddRange(phase.MandatoryCommands ?? []);
            values.AddRange(phase.Evidence ?? []);
        }

        return values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).ToArray();
    }

    private static void ValidateStatus(
        string? status,
        string field,
        ICollection<string> warnings,
        ICollection<string> blockers)
    {
        if (string.IsNullOrWhiteSpace(status) ||
            !AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            blockers.Add($"{field} status must be pass, fail, or not_run.");
            return;
        }

        if (IsStatus(status, "not_run"))
        {
            warnings.Add($"{field} has not been run.");
        }
    }

    private static void Require(string? value, string field, ICollection<string> blockers)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            blockers.Add($"{field} is required.");
        }
    }

    private static void RequireItems(IReadOnlyList<string>? values, string field, ICollection<string> blockers)
    {
        if (values is null || values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
        {
            blockers.Add($"{field} must contain at least one item.");
        }
    }

    private static bool IsStatus(string? status, string expected) =>
        string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);

    private static AssistantEngineerGoalRunValidationResult Result(
        IReadOnlyCollection<string> warnings,
        IReadOnlyCollection<string> blockers) =>
        new(
            IsReady: blockers.Count == 0,
            Status: blockers.Count == 0 ? "PASS" : "FAIL",
            Warnings: warnings.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            Blockers: blockers.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
}
