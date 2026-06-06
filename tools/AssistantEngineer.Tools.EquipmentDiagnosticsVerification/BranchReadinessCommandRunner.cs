using System.Diagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class BranchReadinessCommandRunner
{
    public static IReadOnlyList<BranchReadinessCommandResult> RunRequiredChecks(string repoRoot)
    {
        var commands = new[]
        {
            ("dotnet-restore", "dotnet", new[] { "restore", "AssistantEngineer.sln" }),
            ("dotnet-build", "dotnet", new[] { "build", "AssistantEngineer.sln", "--no-restore" }),
            ("dotnet-test", "dotnet", new[] { "test", "AssistantEngineer.sln", "--no-build" })
        };
        var results = new List<BranchReadinessCommandResult>();

        foreach (var (name, executable, arguments) in commands)
        {
            var result = Run(repoRoot, name, executable, arguments);
            results.Add(result);
            if (!result.Passed)
            {
                break;
            }
        }

        return results;
    }

    private static BranchReadinessCommandResult Run(
        string repoRoot,
        string name,
        string executable,
        IReadOnlyList<string> arguments)
    {
        var command = $"{executable} {string.Join(' ', arguments)}";
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException($"Failed to start '{command}'.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(outputTask, errorTask);
        var summary = process.ExitCode == 0
            ? "Passed."
            : FirstMeaningfulFailureLine(errorTask.Result, outputTask.Result);

        return new BranchReadinessCommandResult(
            Name: name,
            Command: command,
            Passed: process.ExitCode == 0,
            ExitCode: process.ExitCode,
            Summary: summary);
    }

    private static string FirstMeaningfulFailureLine(params string[] outputs) =>
        outputs
            .SelectMany(output => output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            .Select(line => line.Trim())
            .FirstOrDefault(line =>
                line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("не пройден", StringComparison.OrdinalIgnoreCase))
        ?? "Command failed; rerun it directly for detailed output.";
}
