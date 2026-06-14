namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal sealed record EquipmentDiagnosticsVerificationOptions(
    string Command,
    string? RepositoryRoot,
    string BaseRef,
    string Scope,
    string? OutputDirectory,
    string? ReportPath,
    string? OutputPath,
    string? MarkdownOutputPath,
    bool SkipCommandChecks,
    bool ShowHelp)
{
    public static EquipmentDiagnosticsVerificationOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new("full-report", null, "origin/master", "EquipmentDiagnostics", null, null, null, null, false, false);
        }

        if (args.Any(arg => arg is "-h" or "--help" or "help"))
        {
            return new("full-report", null, "origin/master", "EquipmentDiagnostics", null, null, null, null, false, true);
        }

        var command = args[0];
        string? repositoryRoot = null;
        var baseRef = "origin/master";
        var scope = "EquipmentDiagnostics";
        string? outputDirectory = null;
        string? reportPath = null;
        string? outputPath = null;
        string? markdownOutputPath = null;
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

            if (args[index] == "--report")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--report requires a path.");
                }

                reportPath = args[++index];
                continue;
            }

            if (args[index] == "--output")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--output requires a path.");
                }

                outputPath = args[++index];
                continue;
            }

            if (args[index] == "--markdown-output")
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("--markdown-output requires a path.");
                }

                markdownOutputPath = args[++index];
                continue;
            }

            throw new InvalidOperationException($"Unsupported option '{args[index]}'.");
        }

        return new(command, repositoryRoot, baseRef, scope, outputDirectory, reportPath, outputPath, markdownOutputPath, skipCommandChecks, false);
    }
}
