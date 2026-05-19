using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Production;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Staging;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed class OwnershipBackfillCommandLineParseResult
{
    public static OwnershipBackfillCommandLineParseResult DryRunSuccess(OwnershipBackfillOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.DryRun,
            Options = options
        };

    public static OwnershipBackfillCommandLineParseResult ValidateEvidenceSuccess(OwnershipBackfillGateOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.ValidateEvidence,
            GateOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult ApplySuccess(OwnershipBackfillApplyOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.Apply,
            ApplyOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult ValidateApplyReadinessSuccess(OwnershipBackfillApplyReadinessOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.ValidateApplyReadiness,
            ApplyReadinessOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult ValidateProductionPromotionSuccess(OwnershipBackfillProductionPromotionOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.ValidateProductionPromotion,
            ProductionPromotionOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult ValidateStagingPreflightSuccess(OwnershipBackfillStagingApplyPreflightOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.ValidateStagingPreflight,
            StagingPreflightOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult ValidateStagingAcceptanceSuccess(OwnershipBackfillStagingAcceptanceOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.ValidateStagingAcceptance,
            StagingAcceptanceOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult PlanApplySuccess(OwnershipBackfillPlanOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.PlanApply,
            PlanOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult SignoffPlanSuccess(OwnershipBackfillPlanSignoffOptions options) =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            CommandType = OwnershipBackfillCommandType.SignoffPlan,
            SignoffOptions = options
        };

    public static OwnershipBackfillCommandLineParseResult Help() =>
        new()
        {
            IsSuccess = true,
            ExitCode = 0,
            ShowHelp = true
        };

    public static OwnershipBackfillCommandLineParseResult Failure(string message, int exitCode = 1) =>
        new()
        {
            IsSuccess = false,
            ExitCode = exitCode,
            ErrorMessage = message
        };

    public bool IsSuccess { get; private init; }
    public bool ShowHelp { get; private init; }
    public int ExitCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public OwnershipBackfillCommandType? CommandType { get; private init; }
    public OwnershipBackfillOptions? Options { get; private init; }
    public OwnershipBackfillGateOptions? GateOptions { get; private init; }
    public OwnershipBackfillApplyOptions? ApplyOptions { get; private init; }
    public OwnershipBackfillApplyReadinessOptions? ApplyReadinessOptions { get; private init; }
    public OwnershipBackfillProductionPromotionOptions? ProductionPromotionOptions { get; private init; }
    public OwnershipBackfillStagingApplyPreflightOptions? StagingPreflightOptions { get; private init; }
    public OwnershipBackfillStagingAcceptanceOptions? StagingAcceptanceOptions { get; private init; }
    public OwnershipBackfillPlanOptions? PlanOptions { get; private init; }
    public OwnershipBackfillPlanSignoffOptions? SignoffOptions { get; private init; }
}
