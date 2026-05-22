namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed record OwnershipBackfillCommandParseError(string Message)
{
    public static OwnershipBackfillCommandParseError MissingValue(string optionName) =>
        new(OwnershipBackfillConsoleRedactor.RedactText($"{optionName} requires a value."));

    public static OwnershipBackfillCommandParseError UnknownOption(string optionName) =>
        new(OwnershipBackfillConsoleRedactor.RedactText($"Unknown option: {optionName}"));
}
