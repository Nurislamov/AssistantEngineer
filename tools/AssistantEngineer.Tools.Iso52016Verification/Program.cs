using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.Iso52016Verification;

internal static class Program
{
    private const string RegistryRelativePath = "docs/verification/Iso52016VerificationRegistry.json";
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(IsHelp))
            {
                PrintHelp();
                return 0;
            }

            var command = CommandOptions.Parse(args);
            var repoRoot = ResolveRepositoryRoot(command.RepoRoot);
            Directory.SetCurrentDirectory(repoRoot);

            var registry = LoadRegistry(repoRoot);
            var verifier = new Iso52016Verifier(repoRoot, registry);

            switch (command.Name)
            {
                case "list-stages":
                    verifier.ListStages();
                    return 0;

                case "verify-stage":
                    verifier.VerifyStage(command.RequireStageId(), command.SkipTests);
                    return 0;

                case "verify-all":
                    verifier.VerifyAll(command.SkipTests, releaseReady: false, requireCleanGit: false);
                    return 0;

                case "assert-release-ready":
                    verifier.VerifyAll(command.SkipTests, releaseReady: true, command.RequireCleanGit);
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

    private sealed class Iso52016Verifier
    {
        private static readonly string[] ForbiddenWrapperTokens =
        {
            "requiredFiles = @(",
            "Assert-NoForbiddenPositiveClaims",
            "dotnet test",
            "Invoke-RepoScript",
            "Invoke-RepoCommand",
            "ConvertFrom-Json",
            "BEGIN ISO52016",
            "BEGIN AE-ISO52016",
            "CONTRACT HOOK",
            "literal hook",
            "literal contract",
            "SkipPhysical"
        };

        private static readonly string[] ForbiddenPositiveClaims =
        {
            "full ISO 52016 parity",
            "ISO52016 parity",
            "complete ISO 52016 numerical equivalence",
            "complete ISO52016 numerical equivalence",
            "full pyBuildingEnergy parity",
            "pyBuildingEnergy parity",
            "pyBuildingEnergy numerical equivalence",
            "full EnergyPlus parity",
            "EnergyPlus parity",
            "EnergyPlus numerical equivalence",
            "ASHRAE 140 validation",
            "ASHRAE Standard 140 validation",
            "ASHRAE Standard 140 benchmark-grade claim",
            "complete ISO52010 compliance",
            "complete ISO52016 compliance"
        };

        private readonly string repoRoot;
        private readonly Iso52016VerificationRegistry registry;

        public Iso52016Verifier(string repoRoot, Iso52016VerificationRegistry registry)
        {
            this.repoRoot = repoRoot;
            this.registry = registry;
        }

        public void ListStages()
        {
            VerifyRegistryShape();

            foreach (var stage in registry.Stages)
                Console.WriteLine($"{stage.Id}  {stage.Name}");
        }

        public void VerifyStage(string stageId, bool skipTests)
        {
            VerifyRegistryShape();
            var stage = registry.Stages.SingleOrDefault(item => string.Equals(item.Id, stageId, StringComparison.Ordinal));
            if (stage is null)
                throw new InvalidOperationException($"Unknown ISO52016 verification stage id: {stageId}");

            Console.WriteLine($"ISO52016 verification stage: {stage.Id} - {stage.Name}");
            VerifyStageFiles(stage);
            VerifyStageManifests(stage);
            VerifyClaimBoundaries(new[] { stage });
            VerifyNoPositiveParityClaims(new[] { stage });
            VerifyGeneratedArtifactPolicy(new[] { stage }, requireGitCleanlinessForArtifacts: true);
            VerifyWrapperScripts(new[] { stage });

            if (!skipTests)
                RunStageTests(new[] { stage });

            WriteSuccess($"ISO52016 stage verification passed: {stage.Id}");
        }

        public void VerifyAll(bool skipTests, bool releaseReady, bool requireCleanGit)
        {
            VerifyRegistryShape();
            Console.WriteLine(releaseReady
                ? "ISO52016 release-ready assertion"
                : "ISO52016 verification");
            Console.WriteLine($"Repository: {repoRoot}");
            Console.WriteLine("Claim boundary: validation/internal engineering anchors only.");

            VerifyRegistryFile();
            VerifyRequiredFiles();
            VerifyAllManifestsParse();
            VerifyClaimBoundaries(registry.Stages);
            VerifyNoPositiveParityClaims(registry.Stages);
            VerifyGeneratedArtifactPolicy(registry.Stages, requireGitCleanlinessForArtifacts: true);
            VerifyWrapperScripts(registry.Stages);

            if (releaseReady)
            {
                VerifyReleaseReadyManifests();
                if (requireCleanGit)
                    VerifyCleanGit();
            }

            if (!skipTests)
                RunStageTests(registry.Stages);

            WriteSuccess(releaseReady
                ? "ISO52016 release-ready assertion passed."
                : "ISO52016 verification passed.");
        }

        private void VerifyRegistryShape()
        {
            if (string.IsNullOrWhiteSpace(registry.RegistryId))
                throw new InvalidOperationException("Registry id is required.");

            if (registry.Stages.Count == 0)
                throw new InvalidOperationException("Registry must define at least one stage.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stage in registry.Stages)
            {
                RequireText(stage.Id, "Stage id is required.");
                RequireText(stage.Name, $"Stage name is required for {stage.Id}.");
                RequireText(stage.Scope, $"Stage scope is required for {stage.Id}.");
                if (!ids.Add(stage.Id))
                    throw new InvalidOperationException($"Duplicate ISO52016 verification stage id: {stage.Id}");

                VerifyClaimBoundary(stage);
            }
        }

        private void VerifyRegistryFile()
        {
            RequireFile(RegistryRelativePath);
        }

        private void VerifyRequiredFiles()
        {
            foreach (var stage in registry.Stages)
                VerifyStageFiles(stage);
        }

        private void VerifyStageFiles(Iso52016VerificationStage stage)
        {
            foreach (var path in stage.RelatedManifests
                         .Concat(stage.RequiredDocs)
                         .Concat(stage.RequiredSourceFiles)
                         .Concat(stage.RequiredTestFiles)
                         .Concat(stage.EntrypointWrapperScripts)
                         .Concat(stage.DeprecatedWrapperAliases.Select(alias => alias.Path)))
            {
                RequireFile(path);
            }
        }

        private void VerifyAllManifestsParse()
        {
            foreach (var manifest in registry.Stages.SelectMany(stage => stage.RelatedManifests).Distinct(PathComparer))
                VerifyJsonFile(manifest);
        }

        private void VerifyStageManifests(Iso52016VerificationStage stage)
        {
            foreach (var manifest in stage.RelatedManifests)
                VerifyJsonFile(manifest);
        }

        private void VerifyReleaseReadyManifests()
        {
            foreach (var manifest in registry.ReleaseReadyManifests)
            {
                RequireFile(manifest);
                VerifyJsonFile(manifest);
            }
        }

        private void VerifyClaimBoundaries(IReadOnlyCollection<Iso52016VerificationStage> stages)
        {
            foreach (var stage in stages)
                VerifyClaimBoundary(stage);
        }

        private void VerifyClaimBoundary(Iso52016VerificationStage stage)
        {
            if (stage.ClaimBoundary.Count == 0)
                throw new InvalidOperationException($"Stage {stage.Id} must define a claim boundary.");

            foreach (var nonClaim in registry.RequiredNonClaims.Concat(stage.RequiredNonClaims))
            {
                if (!stage.ClaimBoundary.Any(item => string.Equals(item, nonClaim, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Stage {stage.Id} claim boundary is missing: {nonClaim}");
            }
        }

        private void VerifyNoPositiveParityClaims(IReadOnlyCollection<Iso52016VerificationStage> stages)
        {
            var files = stages
                .SelectMany(stage => stage.RelatedManifests
                    .Concat(stage.RequiredDocs)
                    .Concat(stage.RequiredTestFiles))
                .Append(RegistryRelativePath)
                .Distinct(PathComparer);

            foreach (var path in files)
                AssertNoForbiddenPositiveClaims(path);
        }

        private void VerifyGeneratedArtifactPolicy(
            IReadOnlyCollection<Iso52016VerificationStage> stages,
            bool requireGitCleanlinessForArtifacts)
        {
            var generatedPaths = registry.GeneratedArtifactPaths
                .Concat(stages.SelectMany(stage => stage.GeneratedArtifactPaths))
                .Distinct(PathComparer)
                .ToArray();

            if (generatedPaths.Length == 0)
                throw new InvalidOperationException("Registry must define generated artifact paths.");

            foreach (var relativePath in generatedPaths)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    throw new InvalidOperationException("Generated artifact path cannot be empty.");
            }

            if (!requireGitCleanlinessForArtifacts)
                return;

            foreach (var relativePath in generatedPaths)
            {
                var output = RunProcessCapture("git", $"ls-files -- {Quote(relativePath)}", repoRoot);
                if (!string.IsNullOrWhiteSpace(output.StdOut))
                    throw new InvalidOperationException($"Generated ISO52016 artifacts are tracked by git under {relativePath}: {output.StdOut.Trim()}");
            }
        }

        private void VerifyWrapperScripts(IReadOnlyCollection<Iso52016VerificationStage> stages)
        {
            var entrypointScripts = registry.EntrypointWrapperScripts
                .Concat(stages.SelectMany(stage => stage.EntrypointWrapperScripts))
                .Distinct(PathComparer);

            foreach (var script in entrypointScripts)
                VerifyThinWrapper(script, requireStageCommand: false);

            foreach (var alias in registry.DeprecatedWrapperAliases
                         .Concat(stages.SelectMany(stage => stage.DeprecatedWrapperAliases)))
            {
                VerifyThinWrapper(alias.Path, requireStageCommand: true);
                var text = ReadRepoFile(alias.Path);

                if (!text.Contains("verify-stage", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Stage wrapper must call verify-stage: {alias.Path}");

                if (!text.Contains(alias.StageId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Stage wrapper {alias.Path} must pass stage id {alias.StageId}.");
            }
        }

        private void VerifyThinWrapper(string relativePath, bool requireStageCommand)
        {
            RequireFile(relativePath);
            var script = ReadRepoFile(relativePath);

            if (!script.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", StringComparison.Ordinal))
                throw new InvalidOperationException($"PowerShell wrapper must delegate to ISO52016 C# verification tool: {relativePath}");

            if (!script.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"PowerShell wrapper must call dotnet: {relativePath}");

            if (requireStageCommand && !script.Contains("--stage-id", StringComparison.Ordinal))
                throw new InvalidOperationException($"Stage wrapper must pass --stage-id: {relativePath}");

            foreach (var token in ForbiddenWrapperTokens)
            {
                if (script.Contains(token, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"PowerShell wrapper {relativePath} contains orchestration token: {token}");
            }
        }

        private void RunStageTests(IReadOnlyCollection<Iso52016VerificationStage> stages)
        {
            var filters = stages
                .SelectMany(stage => stage.TestFilters)
                .Where(filter => !string.IsNullOrWhiteSpace(filter))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (filters.Length == 0)
                return;

            var filter = string.Join("|", filters);
            Console.WriteLine();
            Console.WriteLine($"=> dotnet test ISO52016 filters: {filter}");

            var exitCode = RunProcess("dotnet", $"test .\\tests\\AssistantEngineer.Tests\\AssistantEngineer.Tests.csproj --filter {Quote(filter)}", repoRoot);
            if (exitCode != 0)
                throw new InvalidOperationException($"dotnet test failed with exit code {exitCode} for filter: {filter}");
        }

        private void VerifyCleanGit()
        {
            var output = RunProcessCapture("git", "status --porcelain", repoRoot);
            if (!string.IsNullOrWhiteSpace(output.StdOut))
                throw new InvalidOperationException("Working tree is not clean. Commit or stash changes before release-ready assertion.");
        }

        private void VerifyJsonFile(string relativePath)
        {
            RequireFile(relativePath);
            try
            {
                using var _ = JsonDocument.Parse(ReadRepoFile(relativePath));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Invalid JSON file {relativePath}: {exception.Message}");
            }
        }

        private void AssertNoForbiddenPositiveClaims(string relativePath)
        {
            RequireFile(relativePath);

            var lines = File.ReadLines(ResolvePath(relativePath)).ToArray();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var claim in ForbiddenPositiveClaims)
                {
                    if (!line.Contains(claim, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (LineNegatesClaim(line, claim))
                        continue;

                    throw new InvalidOperationException(
                        $"Forbidden positive claim found in {relativePath} line {i + 1}: {line.Trim()}");
                }
            }
        }

        private static bool LineNegatesClaim(string line, string claim)
        {
            var lineLower = line.ToLowerInvariant();
            var claimLower = claim.ToLowerInvariant();
            var index = lineLower.IndexOf(claimLower, StringComparison.Ordinal);
            if (index < 0)
                return false;

            var prefix = lineLower[..index];
            return prefix.Contains("not ", StringComparison.Ordinal) ||
                   prefix.Contains("no ", StringComparison.Ordinal) ||
                   prefix.Contains("without ", StringComparison.Ordinal) ||
                   prefix.Contains("does not ", StringComparison.Ordinal) ||
                   prefix.Contains("doesn't ", StringComparison.Ordinal) ||
                   prefix.Contains("must not ", StringComparison.Ordinal) ||
                   prefix.Contains("doesnotcontain", StringComparison.Ordinal);
        }

        private string ReadRepoFile(string relativePath) =>
            File.ReadAllText(ResolvePath(relativePath));

        private void RequireFile(string relativePath)
        {
            var path = ResolvePath(relativePath);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required ISO52016 verification file is missing: {relativePath}", path);
        }

        private string ResolvePath(string relativePath) =>
            Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        private static void RequireText(string? value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(message);
        }
    }

    private static int RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveProcessFileName(fileName),
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, data) =>
        {
            if (data.Data is not null)
                Console.WriteLine(data.Data);
        };
        process.ErrorDataReceived += (_, data) =>
        {
            if (data.Data is not null)
                Console.Error.WriteLine(data.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;
    }

    private static ProcessOutput RunProcessCapture(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveProcessFileName(fileName),
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} {arguments} failed with exit code {process.ExitCode}: {stderr}");

        return new ProcessOutput(stdout, stderr);
    }

    private static string ResolveProcessFileName(string fileName)
    {
        if (!OperatingSystem.IsWindows())
            return fileName;

        if (string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase))
            return FindExecutableOnPath("dotnet", ".exe", ".cmd", ".bat") ?? "dotnet.exe";

        if (string.Equals(fileName, "git", StringComparison.OrdinalIgnoreCase))
            return FindExecutableOnPath("git", ".exe", ".cmd", ".bat") ?? "git.exe";

        return fileName;
    }

    private static string? FindExecutableOnPath(string fileName, params string[] extensions)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedDirectory = directory.Trim('"');
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(normalizedDirectory, fileName + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static string Quote(string value) =>
        "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private sealed record ProcessOutput(string StdOut, string StdErr);

    private sealed record CommandOptions(
        string Name,
        string? RepoRoot,
        bool SkipTests,
        bool RequireCleanGit,
        string? StageId)
    {
        public static CommandOptions Parse(IReadOnlyList<string> args)
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

            return new CommandOptions(command, repoRoot, skipTests, requireCleanGit, stageId);
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
}

internal sealed record Iso52016VerificationRegistry
{
    [JsonPropertyName("registryId")]
    public string RegistryId { get; init; } = string.Empty;

    [JsonPropertyName("requiredNonClaims")]
    public IReadOnlyList<string> RequiredNonClaims { get; init; } = Array.Empty<string>();

    [JsonPropertyName("generatedArtifactPaths")]
    public IReadOnlyList<string> GeneratedArtifactPaths { get; init; } = Array.Empty<string>();

    [JsonPropertyName("releaseReadyManifests")]
    public IReadOnlyList<string> ReleaseReadyManifests { get; init; } = Array.Empty<string>();

    [JsonPropertyName("entrypointWrapperScripts")]
    public IReadOnlyList<string> EntrypointWrapperScripts { get; init; } = Array.Empty<string>();

    [JsonPropertyName("deprecatedWrapperAliases")]
    public IReadOnlyList<Iso52016WrapperAlias> DeprecatedWrapperAliases { get; init; } = Array.Empty<Iso52016WrapperAlias>();

    [JsonPropertyName("stages")]
    public IReadOnlyList<Iso52016VerificationStage> Stages { get; init; } = Array.Empty<Iso52016VerificationStage>();
}

internal sealed record Iso52016VerificationStage
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = string.Empty;

    [JsonPropertyName("relatedManifests")]
    public IReadOnlyList<string> RelatedManifests { get; init; } = Array.Empty<string>();

    [JsonPropertyName("requiredDocs")]
    public IReadOnlyList<string> RequiredDocs { get; init; } = Array.Empty<string>();

    [JsonPropertyName("requiredSourceFiles")]
    public IReadOnlyList<string> RequiredSourceFiles { get; init; } = Array.Empty<string>();

    [JsonPropertyName("requiredTestFiles")]
    public IReadOnlyList<string> RequiredTestFiles { get; init; } = Array.Empty<string>();

    [JsonPropertyName("testFilters")]
    public IReadOnlyList<string> TestFilters { get; init; } = Array.Empty<string>();

    [JsonPropertyName("generatedArtifactPaths")]
    public IReadOnlyList<string> GeneratedArtifactPaths { get; init; } = Array.Empty<string>();

    [JsonPropertyName("requiredNonClaims")]
    public IReadOnlyList<string> RequiredNonClaims { get; init; } = Array.Empty<string>();

    [JsonPropertyName("claimBoundary")]
    public IReadOnlyList<string> ClaimBoundary { get; init; } = Array.Empty<string>();

    [JsonPropertyName("entrypointWrapperScripts")]
    public IReadOnlyList<string> EntrypointWrapperScripts { get; init; } = Array.Empty<string>();

    [JsonPropertyName("deprecatedWrapperAliases")]
    public IReadOnlyList<Iso52016WrapperAlias> DeprecatedWrapperAliases { get; init; } = Array.Empty<Iso52016WrapperAlias>();
}

internal sealed record Iso52016WrapperAlias
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("stageId")]
    public string StageId { get; init; } = string.Empty;
}
