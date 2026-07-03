using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXManualBoundInventoryTests
{
    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");

    private static readonly HashSet<string> DetailedProcedureAvailableCodes = new(StringComparer.Ordinal)
    {
        "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "bA", "bd", "bJ", "bn",
        "E1", "E2", "E3", "E4", "Ed",
        "F0", "F1", "F3", "F5", "F6", "F7", "F8", "F9", "FA", "Fb", "FC", "Fd", "FE", "FF", "FH", "FJ", "FL", "Fn", "FU",
        "H0", "H1", "H2", "H3", "H5", "H6", "H7", "H8", "H9", "HC", "HH", "HJ", "HL",
        "J0", "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9",
        "P0", "P1", "P2", "P3", "P5", "P6", "P7", "P8", "P9", "PC", "PH", "PJ", "PL",
        "d1", "d2", "d3", "d4", "d6", "d7", "d9", "dA", "dC", "dd", "dF", "dH", "dJ", "dL", "dn", "dP", "dU",
        "L0", "L1", "L3", "L4", "L5", "L7", "L9", "LA", "LC", "LF", "LL", "LU",
        "o3", "o7", "o8", "o9", "y7", "y8", "yA",
        "C0", "C2", "C3", "C4", "C5", "C6", "Cb", "CC", "Cd", "CE", "CF", "CH", "CJ", "CL", "Cn", "CP", "Cy",
        "U0", "U2", "U3", "U4", "U6", "U8", "U9", "UE", "UF", "UL"
    };

    private static readonly HashSet<string> StatusOrPromptCodes = new(StringComparer.Ordinal)
    {
        "A0", "A2", "A3", "A4", "A6", "A7", "A8", "Ab", "AC", "Ad", "AE", "AF", "AH", "AJ", "AP", "AU",
        "C8", "C9", "CA",
        "db",
        "n0", "n2", "n4", "n6", "n7", "n8", "n9", "nA", "nC", "nE", "nF", "nH",
        "UC"
    };

    private static readonly HashSet<string> RepairedDetailedCodes = new(StringComparer.Ordinal)
    {
        "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "bA", "bd", "bJ", "bn",
        "E1", "E2", "E3", "E4", "Ed",
        "F0", "F1", "F3", "F5", "F6", "F7", "F8", "F9", "FA", "Fb", "FC", "Fd", "FE", "FF", "FH", "FJ", "FL", "Fn", "FU",
        "H0", "H1", "H2", "H3", "H5", "H6", "H7", "H8", "H9", "HC", "HH", "HJ", "HL",
        "J0", "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9",
        "P0", "P1", "P2", "P3", "P5", "P6", "P7", "P8", "P9", "PC", "PH", "PJ", "PL",
        "d1", "d2", "d3", "d4", "d6", "d7", "d9", "dA", "dC", "dd", "dF", "dH", "dJ", "dL", "dn", "dP", "dU",
        "L0", "L1", "L3", "L4", "L5", "L7", "L9", "LA", "LC", "LF", "LL", "LU",
        "o3", "o7", "o8", "o9", "y7", "y8", "yA",
        "C0", "C2", "C3", "C4", "C5", "C6", "Cb", "CC", "Cd", "CE", "CF", "CH", "CJ", "CL", "Cn", "CP", "Cy",
        "U0", "U2", "U3", "U4", "U6", "U8", "U9", "UE", "UF", "UL"
    };

    private static readonly HashSet<string> ResolvedManualReviewCodes = new(StringComparer.Ordinal)
    {
        "d5", "d8", "dE", "L2", "L6", "LH"
    };

    private static readonly HashSet<string> ManualSectionNeedsReviewCodes = new(StringComparer.Ordinal);

    private static readonly HashSet<string> RepairedTableOnlyCodes = new(StringComparer.Ordinal)
    {
        "bb", "bE", "bF", "bH", "bP", "bU", "E0", "FP",
        "G0", "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9",
        "GA", "Gb", "GC", "Gd", "GE", "GF", "GH", "GJ", "GL", "Gn", "GP", "GU", "Gy",
        "H4", "HA", "HE", "HF", "HP", "HU",
        "JA", "JC", "JE", "JF", "JL",
        "P4", "PA", "PE", "PF", "PP", "PU",
        "dy", "L8", "Lb", "LE", "LJ", "LP",
        "o0", "o1", "o2", "o4", "o5", "o6", "oA", "ob", "oC",
        "y1", "y2"
    };

    [Fact]
    public void GmvXRuntimeCountsMatchManualBoundInventoryScope()
    {
        var entries = ReadGmvXEntries();

        Assert.Equal(263, entries.Length);
        Assert.Equal(121, entries.Count(entry => entry.Category == "outdoor"));
        Assert.Equal(60, entries.Count(entry => entry.Category == "indoor"));
        Assert.Equal(44, entries.Count(entry => entry.Category == "status"));
        Assert.Equal(38, entries.Count(entry => entry.Category == "debugging"));
        Assert.Equal(1296, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void GmvXInventoryClassificationHasNoConflictOrUnclassifiedRows()
    {
        var classifiedEntries = ReadGmvXEntries()
            .Select(entry => entry with { RepairClass = Classify(entry.Code) })
            .ToArray();

        Assert.DoesNotContain(classifiedEntries, entry => entry.RepairClass == "Conflict");
        Assert.DoesNotContain(classifiedEntries, entry => entry.RepairClass == "Unclassified");

        Assert.Equal(240, classifiedEntries.Count(entry => entry.RepairClass == "AlreadyRepaired"));
        Assert.DoesNotContain(classifiedEntries, entry => entry.RepairClass == "DetailedProcedureAvailable");
        Assert.DoesNotContain(classifiedEntries, entry => entry.RepairClass == "StatusOrPrompt");
        Assert.Equal(23, classifiedEntries.Count(entry => entry.RepairClass == "TableOnlySafe"));
        Assert.DoesNotContain(classifiedEntries, entry => entry.RepairClass == "ManualSectionNeedsReview");

        Assert.Equal(
            ManualSectionNeedsReviewCodes.Order(StringComparer.Ordinal),
            classifiedEntries
                .Where(entry => entry.RepairClass == "ManualSectionNeedsReview")
                .Select(entry => entry.Code)
                .Order(StringComparer.Ordinal));

        Assert.All(new[] { "A0", "A2", "A3", "A4", "AJ", "db", "UC", "b1", "bJ", "bn", "E1", "Ed", "F5", "F9", "FH", "FU", "H0", "H5", "HL", "J0", "J8", "P0", "P9", "PL", "d1", "d2", "dA", "dJ", "dP", "dU", "LL" }, code =>
            Assert.Contains(classifiedEntries, entry => entry.Code == code && entry.RepairClass == "AlreadyRepaired"));
        Assert.All(new[] { "L1", "U0", "d5", "d8", "dE", "L2", "L6", "LH", "bb", "E0", "FP", "G0", "GJ", "Gy", "H4", "JA", "P4", "PU", "dy", "L8", "Lb", "LE", "LJ", "LP", "o0", "oA", "ob", "oC", "y1", "y2" }, code =>
            Assert.Contains(classifiedEntries, entry => entry.Code == code && entry.RepairClass == "AlreadyRepaired"));
    }

    [Fact]
    public void GmvXInventoryRunnerExistsAndStaysInventoryOnly()
    {
        var scriptPath = Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "equipment-diagnostics",
            "invoke-gmvx-manual-bound-closure-inventory.ps1");

        Assert.True(File.Exists(scriptPath), $"Inventory script is missing: {scriptPath}");

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("gmvx-manual-bound-closure-inventory.json", script, StringComparison.Ordinal);
        Assert.Contains("gmvx-manual-bound-closure-inventory.csv", script, StringComparison.Ordinal);
        Assert.Contains("GenericVisibleTemplate", script, StringComparison.Ordinal);
        Assert.Contains("NeedsManualBinding", script, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertFrom-Json -Depth", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Gmv6RuntimeScopeRemainsClosedAndUnchangedByGmvXInventory()
    {
        var gmv6Root = Path.Combine(GreeRoot, "gmv6");
        var gmv6Entries = Directory.GetFiles(gmv6Root, "*.json", SearchOption.AllDirectories)
            .Select(ReadObject)
            .ToArray();

        Assert.Equal(263, gmv6Entries.Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(gmv6Root, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(gmv6Root, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(gmv6Root, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(gmv6Root, "debugging"), "*.json").Length);

        foreach (var entry in gmv6Entries)
        {
            Assert.Equal("GMV6", RequiredString(entry, "series"));
            Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        }
    }

    private static GmvXEntry[] ReadGmvXEntries() =>
        Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path =>
            {
                var entry = ReadObject(path);
                return new GmvXEntry(
                    Code: RequiredString(entry, "code"),
                    Category: new DirectoryInfo(Path.GetDirectoryName(path)!).Name,
                    RepairClass: string.Empty);
            })
            .ToArray();

    private static string Classify(string code)
    {
        if (StatusOrPromptCodes.Contains(code) ||
            RepairedDetailedCodes.Contains(code) ||
            ResolvedManualReviewCodes.Contains(code) ||
            RepairedTableOnlyCodes.Contains(code))
        {
            return "AlreadyRepaired";
        }

        var classCount = 0;
        var repairClass = "TableOnlySafe";

        if (DetailedProcedureAvailableCodes.Contains(code))
        {
            classCount++;
            repairClass = "DetailedProcedureAvailable";
        }

        if (ManualSectionNeedsReviewCodes.Contains(code))
        {
            classCount++;
            repairClass = "ManualSectionNeedsReview";
        }

        return classCount > 1 ? "Conflict" : repairClass;
    }

    private static JsonObject ReadObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        return Assert.IsType<JsonObject>(node);
    }

    private static string RequiredString(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
    }

    private sealed record GmvXEntry(string Code, string Category, string RepairClass);
}
