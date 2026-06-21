using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class ManualCoverageRegistryTests
{
    private static readonly string RegistryPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "manual-library",
        "manuals.json");

    [Fact]
    public void RegistryIsValidAndManualIdsAreUnique()
    {
        using var document = LoadRegistry();
        var manuals = document.RootElement.GetProperty("manuals").EnumerateArray().ToArray();
        var manualIds = manuals
            .Select(manual => manual.GetProperty("manualId").GetString())
            .ToArray();
        var requiredProperties = new[]
        {
            "manualId",
            "fileName",
            "relativeSourcePath",
            "manufacturer",
            "equipmentFamily",
            "equipmentType",
            "series",
            "modelScope",
            "documentTitle",
            "documentCode",
            "sourceLanguage",
            "sourceKind",
            "fileFormat",
            "importStatus",
            "coverageStatus",
            "diagnosticUsefulness",
            "importedPackageIds",
            "entriesImported",
            "categoriesImported",
            "knownSmokeCodes",
            "sourceCommit",
            "latestQualityCommit",
            "productionStatus",
            "notes",
            "futureTelegramManualLibrary"
        };

        Assert.Equal(47, manuals.Length);
        Assert.DoesNotContain(manualIds, string.IsNullOrWhiteSpace);
        Assert.Equal(manualIds.Length, manualIds.Distinct(StringComparer.Ordinal).Count());
        Assert.All(manuals, manual =>
        {
            Assert.All(requiredProperties, property => Assert.True(manual.TryGetProperty(property, out _)));
            var relativePath = manual.GetProperty("relativeSourcePath").GetString();
            Assert.NotNull(relativePath);
            Assert.StartsWith(
                "artifacts/manual-intake/sources/gree/",
                relativePath,
                StringComparison.Ordinal);
            Assert.False(Path.IsPathRooted(relativePath));
        });
    }

    [Fact]
    public void RegistryUsesKnownStatusesAndContainsNoSecretLikeValues()
    {
        using var document = LoadRegistry();
        var root = document.RootElement;
        var importStatuses = root
            .GetProperty("statusModels")
            .GetProperty("importStatus")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var coverageStatuses = root
            .GetProperty("statusModels")
            .GetProperty("coverageStatus")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(root.GetProperty("manuals").EnumerateArray(), manual =>
        {
            Assert.Contains(manual.GetProperty("importStatus").GetString()!, importStatuses);
            Assert.Contains(manual.GetProperty("coverageStatus").GetString()!, coverageStatuses);
        });

        var json = File.ReadAllText(RegistryPath);
        Assert.DoesNotMatch(
            new Regex(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", RegexOptions.CultureInvariant),
            json);
        Assert.DoesNotContain("BotToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WebhookSecret", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportedGmv6ManualMatchesCurrentKnowledgePackagesAndCounts()
    {
        using var document = LoadRegistry();
        var manual = document.RootElement
            .GetProperty("manuals")
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("manualId").GetString() ==
                "gree-gmv6-service-manual-2020-09");
        var packageIds = manual
            .GetProperty("importedPackageIds")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();

        Assert.Equal("Imported", manual.GetProperty("importStatus").GetString());
        Assert.Equal("DiagnosticScopeImported", manual.GetProperty("coverageStatus").GetString());
        Assert.Equal("DeployedAndSmokeVerified", manual.GetProperty("productionStatus").GetString());
        Assert.Equal("GC202001-I", manual.GetProperty("documentCode").GetString());
        Assert.Equal(253, manual.GetProperty("entriesImported").GetInt32());
        Assert.Equal(4, packageIds.Length);
        Assert.All(packageIds, packageId =>
            Assert.True(File.Exists(Path.Combine(
                TestPaths.RepoRoot,
                "data",
                "equipment-diagnostics",
                "error-knowledge",
                "packages",
                $"{packageId}.json"))));
    }

    [Fact]
    public void FutureTelegramLibraryPolicyDeniesConsumerAndAllowsTechnicalRoles()
    {
        using var document = LoadRegistry();
        var manuals = document.RootElement.GetProperty("manuals").EnumerateArray().ToArray();
        var expectedAllowed = new[] { "Installer", "Engineer", "Admin", "Owner" };

        Assert.All(manuals, manual =>
        {
            var policy = manual.GetProperty("futureTelegramManualLibrary");
            var allowed = policy
                .GetProperty("allowedRoles")
                .EnumerateArray()
                .Select(role => role.GetString()!)
                .ToArray();
            var denied = policy
                .GetProperty("deniedRoles")
                .EnumerateArray()
                .Select(role => role.GetString()!)
                .ToArray();

            Assert.Equal(expectedAllowed, allowed);
            Assert.Equal(["Consumer"], denied);
        });
    }

    [Fact]
    public void OnlyGmv6IsImportedAndGmvIduIsNextRecommended()
    {
        using var document = LoadRegistry();
        var manuals = document.RootElement.GetProperty("manuals").EnumerateArray().ToArray();
        var imported = manuals.Where(manual =>
            manual.GetProperty("importStatus").GetString() == "Imported").ToArray();
        var recommended = manuals.Where(manual =>
            manual.GetProperty("recommendedNext").GetBoolean()).ToArray();

        Assert.Equal(
            "gree-gmv6-service-manual-2020-09",
            Assert.Single(imported).GetProperty("manualId").GetString());
        Assert.Equal(
            "gree-gmv-idu-service-manual",
            Assert.Single(recommended).GetProperty("manualId").GetString());
    }

    [Fact]
    public void ManualSourceBinariesAreNotTrackedByGit()
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("artifacts/manual-intake/sources/gree");

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Tracked manual source files:{Environment.NewLine}{output}");
    }

    private static JsonDocument LoadRegistry() =>
        JsonDocument.Parse(File.ReadAllText(RegistryPath));
}
