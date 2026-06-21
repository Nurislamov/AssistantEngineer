using System.Diagnostics;
using System.Text.Json;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeVrfEquipmentCatalogRegistryTests
{
    private static readonly string RegistryPath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "equipment-catalog",
        "gree-vrf-equipment-map.json");

    private static readonly HashSet<string> AllowedCoverageStatuses = new(StringComparer.Ordinal)
    {
        "CatalogIdentified",
        "ManualIdentified",
        "Imported",
        "NeedsReview"
    };

    private static readonly HashSet<string> AllowedDeviceTypes = new(StringComparer.Ordinal)
    {
        "WiredController",
        "InfraredController",
        "InfraredReceiver",
        "CentralController",
        "ZoneController",
        "CommissioningTool",
        "PcDebuggingSoftware",
        "ProtocolConverter",
        "BmsGateway",
        "WifiGateway",
        "CloudGateway",
        "EnergyBillingGateway",
        "RemoteMonitoringGateway",
        "DryContactModule",
        "Other"
    };

    private static readonly HashSet<string> AllowedManualTypes = new(StringComparer.Ordinal)
    {
        "ServiceManual",
        "InstallationManual",
        "OwnerManual",
        "ControllerManual",
        "CommissioningToolManual",
        "GatewayManual",
        "BmsManual"
    };

    [Fact]
    public void RegistryExistsAndHasRequiredIdentity()
    {
        Assert.True(File.Exists(RegistryPath));

        using var document = LoadRegistry();
        var root = document.RootElement;

        Assert.True(root.GetProperty("schemaVersion").GetInt32() > 0);
        Assert.Equal("Gree", root.GetProperty("manufacturer").GetString());
        Assert.Equal("VRF", root.GetProperty("family").GetString());
    }

    [Fact]
    public void SourceCatalogsContainExactlyExpectedLocalCatalogues()
    {
        using var document = LoadRegistry();
        var catalogs = document.RootElement.GetProperty("sourceCatalogs").EnumerateArray().ToArray();
        var fileNames = catalogs.Select(catalog => catalog.GetProperty("fileName").GetString()!).ToArray();

        Assert.Equal(
            ["GMV6 Catalouge.pdf", "141367.pdf", "GMV6 2023 РУС.pdf"],
            fileNames);
        Assert.All(catalogs, catalog =>
        {
            Assert.Equal("ProductCatalogue", catalog.GetProperty("sourceType").GetString());
            Assert.False(catalog.GetProperty("committed").GetBoolean());
            Assert.StartsWith(
                "artifacts/manual-intake/sources/gree/",
                catalog.GetProperty("localIntakePath").GetString(),
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void RegistryIdsAreUniqueInsideEachSection()
    {
        using var document = LoadRegistry();
        var root = document.RootElement;

        AssertUniqueIds(root.GetProperty("sourceCatalogs"), "id");
        AssertUniqueIds(root.GetProperty("series"), "id");
        AssertUniqueIds(root.GetProperty("indoorUnitTypes"), "id");
        AssertUniqueIds(root.GetProperty("controlAndAccessoryDevices"), "id");
        AssertUniqueIds(root.GetProperty("manualSearchBacklog"), "id");
    }

    [Fact]
    public void AllSourceCatalogReferencesPointToKnownCatalogs()
    {
        using var document = LoadRegistry();
        var root = document.RootElement;
        var catalogIds = root
            .GetProperty("sourceCatalogs")
            .EnumerateArray()
            .Select(catalog => catalog.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        AssertSourceReferences(root.GetProperty("series"), catalogIds);
        AssertSourceReferences(root.GetProperty("indoorUnitTypes"), catalogIds);
        AssertSourceReferences(root.GetProperty("controlAndAccessoryDevices"), catalogIds);
    }

    [Fact]
    public void EnumLikeFieldsUseKnownValues()
    {
        using var document = LoadRegistry();
        var root = document.RootElement;

        Assert.All(root.GetProperty("series").EnumerateArray(), series =>
            Assert.Contains(series.GetProperty("coverageStatus").GetString()!, AllowedCoverageStatuses));
        Assert.All(root.GetProperty("controlAndAccessoryDevices").EnumerateArray(), device =>
            Assert.Contains(device.GetProperty("deviceType").GetString()!, AllowedDeviceTypes));
        Assert.All(root.GetProperty("manualSearchBacklog").EnumerateArray(), item =>
        {
            Assert.Contains(item.GetProperty("desiredManualType").GetString()!, AllowedManualTypes);
            Assert.Equal("Needed", item.GetProperty("status").GetString());
            Assert.InRange(item.GetProperty("priority").GetInt32(), 1, 3);
        });
    }

    [Fact]
    public void ExpectedCatalogSeriesArePresent()
    {
        using var document = LoadRegistry();
        var displayNames = document.RootElement
            .GetProperty("series")
            .EnumerateArray()
            .Select(series => series.GetProperty("displayName").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expected = new[]
        {
            "GMV6",
            "GMV6 HR",
            "GMV X",
            "GMV X PRO",
            "GMV9 Flex",
            "GMV5 MAX",
            "GMV Mini Star",
            "GMV5 Mini",
            "GMV5 Slim",
            "GMV5 Home"
        };

        Assert.All(expected, name => Assert.Contains(name, displayNames));
    }

    [Fact]
    public void ExpectedIndoorTypesArePresent()
    {
        using var document = LoadRegistry();
        var ids = document.RootElement
            .GetProperty("indoorUnitTypes")
            .EnumerateArray()
            .Select(type => type.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var expected = new[]
        {
            "duct",
            "cassette",
            "wall_mounted",
            "floor_ceiling",
            "console",
            "column",
            "fresh_air_processing_unit",
            "ahu_kit",
            "erv_with_evaporator"
        };

        Assert.All(expected, id => Assert.Contains(id, ids));
    }

    [Fact]
    public void ControlAndAccessoryDevicesCoverDiagnosticSurfaces()
    {
        using var document = LoadRegistry();
        var deviceTypes = document.RootElement
            .GetProperty("controlAndAccessoryDevices")
            .EnumerateArray()
            .Select(device => device.GetProperty("deviceType").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("WiredController", deviceTypes);
        Assert.Contains("CentralController", deviceTypes);
        Assert.Contains("CommissioningTool", deviceTypes);
        Assert.Contains("PcDebuggingSoftware", deviceTypes);
        Assert.Contains("BmsGateway", deviceTypes);
        Assert.Contains("WifiGateway", deviceTypes);
        Assert.Contains("EnergyBillingGateway", deviceTypes);
        Assert.Contains("RemoteMonitoringGateway", deviceTypes);
    }

    [Fact]
    public void ManualBacklogReferencesKnownSeriesAndDevices()
    {
        using var document = LoadRegistry();
        var root = document.RootElement;
        var seriesIds = root.GetProperty("series")
            .EnumerateArray()
            .Select(series => series.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var deviceIds = root.GetProperty("controlAndAccessoryDevices")
            .EnumerateArray()
            .Select(device => device.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(root.GetProperty("manualSearchBacklog").EnumerateArray(), item =>
        {
            Assert.All(item.GetProperty("relatedSeriesIds").EnumerateArray(), id =>
                Assert.Contains(id.GetString()!, seriesIds));
            Assert.All(item.GetProperty("relatedDeviceIds").EnumerateArray(), id =>
                Assert.Contains(id.GetString()!, deviceIds));
        });
    }

    [Fact]
    public void FutureTelegramManualPolicyDeniesConsumerAndAllowsTechnicalRoles()
    {
        using var document = LoadRegistry();
        var policy = document.RootElement.GetProperty("futureTelegramManualLibraryPolicy");
        var allowedRoles = policy
            .GetProperty("allowedRoles")
            .EnumerateArray()
            .Select(role => role.GetString()!)
            .ToArray();
        var deniedRoles = policy
            .GetProperty("deniedRoles")
            .EnumerateArray()
            .Select(role => role.GetString()!)
            .ToArray();

        Assert.Equal(["Installer", "Engineer", "Admin", "Owner"], allowedRoles);
        Assert.Equal(["Consumer"], deniedRoles);
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

    private static void AssertUniqueIds(JsonElement items, string propertyName)
    {
        var ids = items
            .EnumerateArray()
            .Select(item => item.GetProperty(propertyName).GetString())
            .ToArray();

        Assert.DoesNotContain(ids, string.IsNullOrWhiteSpace);
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    private static void AssertSourceReferences(JsonElement items, HashSet<string> sourceCatalogIds)
    {
        Assert.All(items.EnumerateArray(), item =>
        {
            var referencedIds = item.GetProperty("sourceCatalogIds").EnumerateArray().ToArray();
            Assert.NotEmpty(referencedIds);
            Assert.All(referencedIds, id => Assert.Contains(id.GetString()!, sourceCatalogIds));
        });
    }
}
