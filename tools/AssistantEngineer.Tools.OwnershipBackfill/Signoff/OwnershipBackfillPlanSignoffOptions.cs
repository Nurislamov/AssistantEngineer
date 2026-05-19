namespace AssistantEngineer.Tools.OwnershipBackfill.Signoff;

public sealed class OwnershipBackfillPlanSignoffOptions
{
    public string? PlanPath { get; init; }
    public string? ExpectedPlanHash { get; init; }
    public string? Reviewer { get; init; }
    public string? Ticket { get; init; }
    public string? OutputDirectory { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public string? ConfirmationPhrase { get; init; }
    public bool ForceOverwrite { get; init; }
}
