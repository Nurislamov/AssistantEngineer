namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal sealed record EquipmentDiagnosticsVerificationOptions(
    string Command,
    string? RepositoryRoot,
    string BaseRef,
    string Scope,
    string? OutputDirectory,
    bool SkipCommandChecks,
    bool ShowHelp)
{
    public static EquipmentDiagnosticsVerificationOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new("full-report", null, "origin/master", "EquipmentDiagnostics", null, false, false);
        }

        if (args.Any(arg => arg is "-h" or "--help" or "help"))
        {
            return new("full-report", null, "origin/master", "EquipmentDiagnostics", null, false, true);
        }

        var command = args[0];
        string? repositoryRoot = null;
        var baseRef = "origin/master";
        var scope = "EquipmentDiagnostics";
        string? outputDirectory = null;
        var skipCommandChecks = false;

        for (var index = 1; index < args.Count; index++)
        {
            if (args[index] == "--repo-root")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--repo-root requires a path.");
                }

                repositoryRoot = args[++index];
                continue;
            }

            if (args[index] == "--base-ref")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--base-ref requires a git ref.");
                }

                baseRef = args[++index];
                continue;
            }

            if (args[index] == "--scope")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--scope requires a value.");
                }

                scope = args[++index];
                continue;
            }

            if (args[index] == "--output-directory")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--output-directory requires a path.");
                }

                outputDirectory = args[++index];
                continue;
            }

            if (args[index] == "--skip-command-checks")
            {
                skipCommandChecks = true;
                continue;
            }

            throw new InvalidOperationException($"Unsupported option '{args[index]}'.");
        }

        return new(command, repositoryRoot, baseRef, scope, outputDirectory, skipCommandChecks, false);
    }
}
