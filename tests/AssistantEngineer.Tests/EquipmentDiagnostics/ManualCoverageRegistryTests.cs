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

        Assert.Equal(49, manuals.Length);
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
        Assert.Equal(255, manual.GetProperty("entriesImported").GetInt32());
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
    public void ImportedManualStatusesRecordGmv6IduAndGmvMiniCoverage()
    {
        using var document = LoadRegistry();
        var manuals = document.RootElement.GetProperty("manuals").EnumerateArray().ToArray();
        var imported = manuals.Where(manual =>
            manual.GetProperty("importStatus").GetString() == "Imported").ToArray();
        var recommended = manuals.Where(manual =>
            manual.GetProperty("recommendedNext").GetBoolean()).ToArray();
        var gmvIdu = manuals.Single(manual =>
            manual.GetProperty("manualId").GetString() == "gree-gmv-idu-service-manual");
        var gmvMini = manuals.Single(manual =>
            manual.GetProperty("manualId").GetString() == "gree-gmv-mini-service-manual");

        Assert.Equal(
            [
                "gree-gmv-mini-service-manual",
                "gree-gmv-x-service-manual-2022-09",
                "gree-gmv6-service-manual-2020-09"
            ],
            imported
                .Select(manual => manual.GetProperty("manualId").GetString()!)
                .Order(StringComparer.Ordinal)
                .ToArray());
        Assert.Empty(recommended);
        Assert.Equal("PartiallyImported", gmvIdu.GetProperty("importStatus").GetString());
        Assert.Equal("PartialDiagnosticScopeImported", gmvIdu.GetProperty("coverageStatus").GetString());
        Assert.Equal(0, gmvIdu.GetProperty("entriesImported").GetInt32());
        Assert.Equal(38, gmvIdu.GetProperty("entriesReferenced").GetInt32());
        Assert.Equal("Imported", gmvMini.GetProperty("importStatus").GetString());
        Assert.Equal("DiagnosticScopeImported", gmvMini.GetProperty("coverageStatus").GetString());
        Assert.Equal(136, gmvMini.GetProperty("entriesImported").GetInt32());
        Assert.Equal(31, gmvMini.GetProperty("entriesReferenced").GetInt32());
        Assert.Single(gmvMini.GetProperty("needsReviewCodes").EnumerateArray());
    }

    [Fact]
    public void GmvIduAnalysisRecordsReferenceMergeAndNoImportedEntries()
    {
        using var document = LoadRegistry();
        var manual = document.RootElement
            .GetProperty("manuals")
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("manualId").GetString() ==
                "gree-gmv-idu-service-manual");
        var analysis = manual.GetProperty("analysis");

        Assert.Equal("GC202004-X", manual.GetProperty("documentCode").GetString());
        Assert.Equal("en", manual.GetProperty("sourceLanguage").GetString());
        Assert.Equal("PartiallyImported", manual.GetProperty("importStatus").GetString());
        Assert.Equal("PartialDiagnosticScopeImported", manual.GetProperty("coverageStatus").GetString());
        Assert.Equal(0, manual.GetProperty("entriesImported").GetInt32());
        Assert.Equal(38, manual.GetProperty("entriesReferenced").GetInt32());
        Assert.Equal(19, manual.GetProperty("procedureCodeCountReviewed").GetInt32());
        Assert.Equal(0, manual.GetProperty("procedureTextEntriesUpdated").GetInt32());
        Assert.Empty(manual.GetProperty("needsReviewCodes").EnumerateArray());
        Assert.Equal(
            "gree-gmv6-indoor-fault-codes",
            Assert.Single(manual.GetProperty("importedPackageIds").EnumerateArray()).GetString());
        Assert.Equal(38, analysis.GetProperty("identifiedCodeCount").GetInt32());
        Assert.Equal(19, analysis.GetProperty("detailedProcedureCodeCount").GetInt32());
        Assert.Equal(38, analysis.GetProperty("existingGmv6IndoorOverlapCount").GetInt32());
        Assert.Equal(
            "MergedAsSourceReferencesNoNewEntries",
            analysis.GetProperty("importDecision").GetString());
        Assert.Equal(38, analysis.GetProperty("sourceReferencesMergedCount").GetInt32());
        Assert.Equal(0, analysis.GetProperty("procedureTextEntriesUpdated").GetInt32());

        var identifiedCodes = analysis
            .GetProperty("identifiedCodes")
            .EnumerateArray()
            .Select(code => code.GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCodes = Directory
            .EnumerateFiles(
                Path.Combine(
                    TestPaths.RepoRoot,
                    "data",
                    "equipment-diagnostics",
                    "error-knowledge",
                    "gree",
                    "gmv6",
                    "indoor"),
                "*.json")
            .Select(path =>
            {
                using var entry = JsonDocument.Parse(File.ReadAllText(path));
                return entry.RootElement.GetProperty("code").GetString()!;
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(38, identifiedCodes.Count);
        Assert.All(identifiedCodes, code => Assert.Contains(code, existingCodes));
    }

    [Fact]
    public void GmvMiniAnalysisRecordsFullImportAndE6ReviewBoundary()
    {
        using var document = LoadRegistry();
        var manual = document.RootElement
            .GetProperty("manuals")
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("manualId").GetString() ==
                "gree-gmv-mini-service-manual");
        var analysis = manual.GetProperty("analysis");
        var packageIds = manual
            .GetProperty("importedPackageIds")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();

        Assert.Equal("en", manual.GetProperty("sourceLanguage").GetString());
        Assert.Equal("Imported", manual.GetProperty("importStatus").GetString());
        Assert.Equal("DiagnosticScopeImported", manual.GetProperty("coverageStatus").GetString());
        Assert.Equal(136, manual.GetProperty("entriesImported").GetInt32());
        Assert.Equal(31, manual.GetProperty("entriesReferenced").GetInt32());
        Assert.Single(manual.GetProperty("needsReviewCodes").EnumerateArray());
        Assert.Equal(
            [
                "gree-gmv-mini-vrf-indoor-controller-codes",
                "gree-gmv-mini-vrf-outdoor-protection-codes",
                "gree-gmv-mini-vrf-status-codes"
            ],
            packageIds);
        Assert.Equal("ED-24GEC.12", analysis.GetProperty("analysisStage").GetString());
        Assert.Equal("SERVICE_MANUAL_GMV_MINI.pdf", analysis.GetProperty("sourceFileUsed").GetString());
        Assert.False(analysis.GetProperty("duplicateFileUsed").GetBoolean());
        Assert.Equal(173, analysis.GetProperty("pageCount").GetInt32());
        Assert.Equal(138, analysis.GetProperty("identifiedCodeCount").GetInt32());
        Assert.Equal(136, analysis.GetProperty("entriesImported").GetInt32());
        Assert.Equal(31, analysis.GetProperty("sourceReferencesMergedCount").GetInt32());
        Assert.Equal(1, analysis.GetProperty("needsReviewCodeCount").GetInt32());
        Assert.Equal(
            "FullRuntimeImportWithAliases",
            analysis.GetProperty("importDecision").GetString());
        Assert.Single(analysis.GetProperty("needsReviewCodes").EnumerateArray());
        Assert.Contains(
            "SERVICE_MANUAL_GMV_MINI (1).pdf",
            manual.GetProperty("notes").GetString(),
            StringComparison.Ordinal);

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
