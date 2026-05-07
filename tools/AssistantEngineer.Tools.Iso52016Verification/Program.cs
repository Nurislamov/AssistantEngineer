using System.Text.Json;

namespace AssistantEngineer.Tools.Iso52016Verification;

internal static class Program
{
    private const string RegistryRelativePath = "docs/verification/Iso52016VerificationRegistry.json";

    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(IsHelp))
            {
                PrintHelp();
                return 0;
            }

            var command = Iso52016VerificationCommandOptions.Parse(args);
            var repoRoot = ResolveRepositoryRoot(command.RepoRoot);
            Directory.SetCurrentDirectory(repoRoot);

            var registry = LoadRegistry(repoRoot);
            var runner = new Iso52016VerificationRunner(repoRoot, registry);

            switch (command.Name)
            {
                case "list-stages":
                    runner.ListStages();
                    return 0;

                case "verify-stage":
                    runner.VerifyStage(command.RequireStageId(), command.SkipTests);
                    return 0;

                case "verify-all":
                    runner.VerifyAll(command.SkipTests, releaseReady: false, requireCleanGit: false);
                    return 0;

                case "assert-release-ready":
                    runner.VerifyAll(command.SkipTests, releaseReady: true, command.RequireCleanGit);
                    return 0;

                default:
                    throw new ArgumentException($"Unknown command: {command.Name}");
            }
        }
        catch (Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception.Message);
            Console.ResetColor();
            return 1;
        }
    }

    private static bool IsHelp(string arg) =>
        arg is "-h" or "--help" or "help" or "/?";

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer ISO52016 registry-driven verification tool");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  list-stages");
        Console.WriteLine("  verify-stage --stage-id <id> [--skip-tests] [--repo-root <path>]");
        Console.WriteLine("  verify-all [--skip-tests] [--repo-root <path>]");
        Console.WriteLine("  assert-release-ready [--skip-tests] [--require-clean-git] [--repo-root <path>]");
        Console.WriteLine("  thin wrapper reference: scripts/iso52016/assert-iso52016-physical-model-chain-release-ready.ps1");
        Console.WriteLine();
        Console.WriteLine("Claim boundary: validation/internal engineering anchors only.");
    }

    private static Iso52016VerificationRegistry LoadRegistry(string repoRoot)
    {
        var path = Path.Combine(repoRoot, RegistryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new FileNotFoundException($"ISO52016 verification registry is missing: {RegistryRelativePath}", path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var registry = JsonSerializer.Deserialize<Iso52016VerificationRegistry>(
            File.ReadAllText(path),
            options);

        if (registry is null)
            throw new InvalidOperationException($"ISO52016 verification registry did not parse: {RegistryRelativePath}");

        return registry;
    }

    private static string ResolveRepositoryRoot(string? explicitRepoRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRepoRoot))
        {
            var fullPath = Path.GetFullPath(explicitRepoRoot);
            if (!File.Exists(Path.Combine(fullPath, "AssistantEngineer.sln")))
                throw new InvalidOperationException($"AssistantEngineer.sln was not found under explicit repo root: {fullPath}");

            return fullPath;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }
}
