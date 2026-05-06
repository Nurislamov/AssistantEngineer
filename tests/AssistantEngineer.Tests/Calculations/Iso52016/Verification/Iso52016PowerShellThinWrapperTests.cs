using System.Text.Json;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Verification;

public class Iso52016PowerShellThinWrapperTests
{
    private static readonly string[] ForbiddenTokens =
    {
        "requiredFiles = @(",
        "Assert-NoForbiddenPositiveClaims",
        "dotnet test",
        "BEGIN ISO52016",
        "BEGIN AE-ISO52016",
        "literal hook",
        "literal contract",
        "CONTRACT HOOK",
        "Invoke-RepoScript",
        "Invoke-RepoCommand"
    };

    private static readonly string[] AllowedCommands =
    {
        "verify-all",
        "verify-stage",
        "assert-release-ready",
        "list-stages"
    };

    [Fact]
    public void EveryIso52016PowerShellScript_IsThinWrapper()
    {
        foreach (var path in Directory.GetFiles(IsoScriptDirectory(), "*.ps1", SearchOption.TopDirectoryOnly))
        {
            var script = File.ReadAllText(path);

            Assert.Contains("AssistantEngineer.Tools.Iso52016Verification.csproj", script);
            Assert.Contains("--project", script);
            Assert.Contains("--repo-root", script);
            Assert.Contains("& dotnet", script, StringComparison.OrdinalIgnoreCase);
            Assert.True(
                AllowedCommands.Any(command => script.Contains(command, StringComparison.Ordinal)),
                $"Script does not call supported ISO52016 tool command: {Path.GetFileName(path)}");

            foreach (var token in ForbiddenTokens)
            {
                Assert.DoesNotContain(token, script, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryIso52016PowerShellScript_IsRegistered()
    {
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(ReadIso52016VerificationRegistry());
        var root = document.RootElement;

        foreach (var script in root.GetProperty("entrypointWrapperScripts").EnumerateArray())
            listed.Add(script.GetString()!);

        foreach (var alias in root.GetProperty("deprecatedWrapperAliases").EnumerateArray())
            listed.Add(alias.GetProperty("path").GetString()!);

        foreach (var stage in root.GetProperty("stages").EnumerateArray())
        {
            foreach (var script in stage.GetProperty("entrypointWrapperScripts").EnumerateArray())
                listed.Add(script.GetString()!);

            foreach (var alias in stage.GetProperty("deprecatedWrapperAliases").EnumerateArray())
                listed.Add(alias.GetProperty("path").GetString()!);
        }

        foreach (var path in Directory.GetFiles(IsoScriptDirectory(), "*.ps1", SearchOption.TopDirectoryOnly))
        {
            var relativePath = $"scripts/iso52016/{Path.GetFileName(path)}";
            Assert.Contains(relativePath, listed);
        }
    }

    private static string IsoScriptDirectory() =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "iso52016");
}
