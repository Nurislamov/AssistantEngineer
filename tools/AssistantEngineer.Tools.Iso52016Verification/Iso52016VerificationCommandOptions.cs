namespace AssistantEngineer.Tools.Iso52016Verification;

internal sealed record Iso52016VerificationCommandOptions(
    string Name,
    string? RepoRoot,
    bool SkipTests,
    bool RequireCleanGit,
    string? StageId)
{
    public static Iso52016VerificationCommandOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            throw new ArgumentException("Command is required. Use --help for usage.");

        var command = args[0];
        string? repoRoot = null;
        string? stageId = null;
        var skipTests = false;
        var requireCleanGit = false;

        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--repo-root":
                case "-RepoRoot":
                    repoRoot = ReadValue(args, ref i, arg);
                    break;

                case "--stage-id":
                case "-StageId":
                    stageId = ReadValue(args, ref i, arg);
                    break;

                case "--skip-tests":
                case "-SkipTests":
                    skipTests = true;
                    break;

                case "--require-clean-git":
                case "-RequireCleanGit":
                    requireCleanGit = true;
                    break;

                default:
                    throw new ArgumentException($"Unknown option: {arg}");
            }
        }

        return new Iso52016VerificationCommandOptions(command, repoRoot, skipTests, requireCleanGit, stageId);
    }

    public string RequireStageId()
    {
        if (string.IsNullOrWhiteSpace(StageId))
            throw new ArgumentException("verify-stage requires --stage-id <id>.");

        return StageId;
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
            throw new ArgumentException($"{optionName} requires a value.");

        index++;
        return args[index];
    }
}
