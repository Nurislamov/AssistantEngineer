namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed class OwnershipBackfillParsedArguments
{
    public OwnershipBackfillParsedArguments(
        IReadOnlyDictionary<string, string> values,
        IReadOnlyCollection<string> presentFlags,
        IReadOnlyCollection<string> repeatedOptions)
    {
        Values = values;
        PresentFlags = presentFlags;
        RepeatedOptions = repeatedOptions;
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public IReadOnlyCollection<string> PresentFlags { get; }

    public IReadOnlyCollection<string> RepeatedOptions { get; }

    public bool TryGetValue(string optionName, out string value) =>
        Values.TryGetValue(optionName, out value!);

    public bool HasFlag(string flagName) =>
        PresentFlags.Contains(flagName, StringComparer.OrdinalIgnoreCase);
}
