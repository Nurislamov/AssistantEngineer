namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed class OwnershipBackfillArgumentReader
{
    public static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        if (index + 1 >= args.Count)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    public bool TryParse(
        IReadOnlyList<string> args,
        OwnershipBackfillCommandDescriptor descriptor,
        out OwnershipBackfillParsedArguments parsed,
        out OwnershipBackfillCommandParseError? error)
    {
        var recognizedOptions = descriptor.AllRecognizedArguments
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var flagOptions = descriptor.FlagArguments
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var repeated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];

            if (!option.StartsWith("--", StringComparison.Ordinal))
                continue;

            if (!recognizedOptions.Contains(option))
            {
                parsed = new OwnershipBackfillParsedArguments(values, flags, repeated);
                error = OwnershipBackfillCommandParseError.UnknownOption(option);
                return false;
            }

            if (flagOptions.Contains(option))
            {
                if (!flags.Add(option))
                    repeated.Add(option);

                continue;
            }

            if (!TryReadValue(args, ref index, out var value))
            {
                parsed = new OwnershipBackfillParsedArguments(values, flags, repeated);
                error = OwnershipBackfillCommandParseError.MissingValue(option);
                return false;
            }

            if (!values.TryAdd(option, value))
            {
                repeated.Add(option);
                values[option] = value;
            }
        }

        parsed = new OwnershipBackfillParsedArguments(values, flags, repeated);
        error = null;
        return true;
    }
}
