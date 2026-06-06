using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class BranchReadinessPrBodyWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Write(string repoRoot, string? reportPath, string? outputPath)
    {
        var resolvedReportPath = ResolvePath(
            repoRoot,
            reportPath,
            Path.Combine("artifacts", "verification", "branch-readiness", "branch-readiness-report.json"));
        if (!File.Exists(resolvedReportPath))
        {
            throw new FileNotFoundException("Branch readiness report was not found.", resolvedReportPath);
        }

        var report = JsonSerializer.Deserialize<BranchReadinessReport>(
            File.ReadAllText(resolvedReportPath),
            JsonOptions) ?? throw new InvalidOperationException("Branch readiness report JSON is empty.");
        var resolvedOutputPath = ResolvePath(
            repoRoot,
            outputPath,
            Path.Combine("artifacts", "verification", "branch-readiness", "pr-body.md"));
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
        var relativeReportPath = Path.GetRelativePath(repoRoot, resolvedReportPath).Replace('\\', '/');
        File.WriteAllText(resolvedOutputPath, new BranchReadinessPrBodyGenerator().Render(report, relativeReportPath));

        return resolvedOutputPath;
    }

    private static string ResolvePath(string repoRoot, string? path, string defaultPath) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(path) ? defaultPath : path, repoRoot);
}
