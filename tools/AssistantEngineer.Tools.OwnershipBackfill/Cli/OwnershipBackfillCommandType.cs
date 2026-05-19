namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public enum OwnershipBackfillCommandType
{
    DryRun,
    ValidateEvidence,
    ValidateApplyReadiness,
    ValidateProductionPromotion,
    ValidateStagingPreflight,
    ValidateStagingAcceptance,
    Apply,
    PlanApply,
    SignoffPlan
}
