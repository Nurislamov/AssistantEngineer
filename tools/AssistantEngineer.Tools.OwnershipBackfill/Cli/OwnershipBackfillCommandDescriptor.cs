namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed record OwnershipBackfillCommandDescriptor(
    string Name,
    OwnershipBackfillCommandType CommandType,
    IReadOnlyList<string> RequiredArguments,
    IReadOnlyList<string> OptionalArguments,
    IReadOnlyList<string> FlagArguments,
    bool SupportsHelp,
    bool ApplyEnabled,
    string UsageSummary)
{
    public IEnumerable<string> AllRecognizedArguments =>
        RequiredArguments.Concat(OptionalArguments).Concat(FlagArguments);
}
