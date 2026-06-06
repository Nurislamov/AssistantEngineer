using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticsManualCodeBookTests
{
    private static readonly string[] RepresentativeCodes =
    [
        "L0", "L1", "L3", "L6", "d1", "d3", "d4", "d6", "o1", "o3", "o8",
        "E0", "E1", "E3", "E4", "F0", "F1", "F3", "F5", "J1", "J7", "J8", "J9",
        "P0", "P5", "P6", "P8", "H0", "H5", "H6", "H8", "G0", "G1", "G2", "G8",
        "U0", "U4", "U6", "U8", "U9", "C0", "C2", "C3", "C5", "C7", "CH", "CL",
        "A0", "A3", "A4", "A8", "Ay", "n6", "n7", "n8", "n9", "nb", "nn",
        "qA", "qH", "qC", "qP", "qU"
    ];

    [Fact]
    public void RegistryIncludesAllRequestedManualFilesAndCoverVerifiedCe52Identity()
    {
        using var registry = JsonDocument.Parse(File.ReadAllText(RegistryPath));
        var sources = registry.RootElement.GetProperty("manualSources").EnumerateArray().ToArray();
        var fileNames = sources.Select(source => source.GetProperty("fileName").GetString()).ToHashSet(StringComparer.Ordinal);

        Assert.All(RequestedManualFiles, fileName => Assert.Contains(fileName, fileNames));
        var misleadingFile = sources.Single(source => source.GetProperty("fileName").GetString() == "CE41-24F(C).pdf");
        Assert.Contains("CE52-24/F(C)", misleadingFile.GetProperty("title").GetString());
    }

    [Fact]
    public void CodeBookSchemaAndAllFilesAreValidJsonWithRequiredClassificationFields()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(SchemaPath));
        Assert.Equal("object", schema.RootElement.GetProperty("type").GetString());

        Assert.All(GetCodeBookFiles(), path =>
        {
            using var file = JsonDocument.Parse(File.ReadAllText(path));
            Assert.All(file.RootElement.GetProperty("occurrences").EnumerateArray(), occurrence =>
            {
                foreach (var property in RequiredProperties)
                {
                    Assert.True(occurrence.TryGetProperty(property, out _), $"{path} missing {property}");
                }
            });
        });
    }

    [Fact]
    public void GmvXOwnerManualContainsRepresentativeIndoorOutdoorDebugAndStatusOccurrences()
    {
        var occurrences = ReadOccurrences()
            .Where(value => value.GetProperty("manualId").GetString() == "gree-gmv-x-owner-manual")
            .ToArray();
        var codes = occurrences.Select(value => value.GetProperty("code").GetString()).ToHashSet(StringComparer.Ordinal);

        Assert.All(RepresentativeCodes, code => Assert.Contains(code, codes));
        Assert.All(occurrences.Where(value => StatusLikeCodes.Contains(value.GetProperty("code").GetString()!)),
            value => Assert.NotEqual("Fault", value.GetProperty("codeKind").GetString()));
    }

    [Fact]
    public void ControllerToolAndTechnicalGuideOccurrencesRemainReferenceOnly()
    {
        var occurrences = ReadOccurrences();

        Assert.All(occurrences.Where(value => value.GetProperty("equipmentSide").GetString() is "Controller" or "CommissioningTool"),
            value =>
            {
                Assert.Equal("ToolFunction", value.GetProperty("codeKind").GetString());
                Assert.Equal("ReferenceOnly", value.GetProperty("promotionReadiness").GetString());
            });
        Assert.Contains(occurrences, value =>
            value.GetProperty("evidenceLevel").GetString() == "TechnicalGuideApplicability" &&
            value.GetProperty("promotionReadiness").GetString() == "ReferenceOnly");
        Assert.DoesNotContain(occurrences, value =>
            value.GetProperty("code").GetString() is "CE41" or "CE42" or "CE52");
    }

    [Fact]
    public void CodeBookIsNotRuntimeAndVerificationReadinessPasses()
    {
        var runtimeEntries = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();
        var documents = GetCodeBookFiles().Select(path => new EquipmentDiagnosticsVerificationDocument(
            Path.GetRelativePath(TestPaths.RepoRoot, path).Replace('\\', '/'),
            File.ReadAllText(path),
            EquipmentDiagnosticsVerificationDocumentKind.ManualCodeBook)).ToArray();
        using var registry = JsonDocument.Parse(File.ReadAllText(RegistryPath));
        var manualIds = registry.RootElement.GetProperty("manualSources").EnumerateArray()
            .Select(source => source.GetProperty("manualId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var report = new EquipmentDiagnosticsVerificationService().Verify(new EquipmentDiagnosticsVerificationInput(
            runtimeEntries, [], [], [], 0, manualIds, documents));

        Assert.True(report.IsReleaseReady);
        Assert.Equal(ReadOccurrences().Count, report.ManualCodeBookSummary.CodeOccurrenceCount);
        Assert.Equal(0, report.ManualCodeBookSummary.DuplicateOrConflictCount);
        Assert.DoesNotContain(EquipmentDiagnosticsJsonKnowledgeSource.GetEmbeddedKnowledgeResourceNames(),
            name => name.Contains(".Knowledge.manual-codebook.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(runtimeEntries, entry => entry.Confidence == DiagnosticConfidence.ManualVerified);
    }

    private static IReadOnlyList<JsonElement> ReadOccurrences() =>
        GetCodeBookFiles().SelectMany(path =>
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("occurrences").EnumerateArray().Select(value => value.Clone()).ToArray();
        }).ToArray();

    private static IReadOnlyList<string> GetCodeBookFiles() =>
        Directory.GetFiles(CodeBookRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase)).ToArray();

    private static readonly string[] RequestedManualFiles =
    [
        "Service Manual for GMV6 v_2020.09.pdf", "Owners-Manual-for GMV6.pdf", "Сервис мануал GREE VRF.pdf",
        "SERVICE_MANUAL_GMV_MINI.pdf", "CE41-24F(C).pdf", "DC Inverter Multi VRF System F sesies.pdf",
        "Manual Portable Commissioning Tool CE41-24F(C).pdf", "DC Inverter Multi VRF System C sesies.pdf",
        "SERVICE_MANUAL_GMV_IDU.pdf", "CE52-24F(C).pdf", "Owner's Manual GMV X DC Inverter VRF Units.pdf",
        "CE42-24_F(C)  v2020.10.29.pdf", "Technical Sales Guide GMV X DC Inverter VRF Units.pdf"
    ];
    private static readonly string[] RequiredProperties =
        ["manualId", "sourceFileName", "sourceTitle", "page", "section", "code", "normalizedCode", "codeKind",
            "equipmentSide", "displayContext", "series", "meaning", "canBecomeDiagnosticCase", "promotionReadiness", "evidenceLevel"];
    private static readonly HashSet<string> StatusLikeCodes =
        ["A0", "A3", "A4", "A8", "Ay", "n6", "n7", "n8", "n9", "nb", "nn", "qA", "qH", "qC", "qP", "qU"];
    private static string CodeBookRoot => Path.Combine(TestPaths.RepoRoot, "src", "Backend",
        "AssistantEngineer.Modules.EquipmentDiagnostics", "Knowledge", "manual-codebook");
    private static string SchemaPath => Path.Combine(CodeBookRoot, "equipment-diagnostics-manual-codebook.schema.json");
    private static string RegistryPath => Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "manual-sources", "gree-manual-sources.json");
}
