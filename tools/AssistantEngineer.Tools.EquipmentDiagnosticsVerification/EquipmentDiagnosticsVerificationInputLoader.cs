using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class EquipmentDiagnosticsVerificationInputLoader
{
    public static EquipmentDiagnosticsVerificationInput Load(string repoRoot)
    {
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();
        var runtimeDocuments = GetRuntimeCatalogFiles(repoRoot)
            .Select(path => ReadDocument(repoRoot, path, EquipmentDiagnosticsVerificationDocumentKind.RuntimeCatalog))
            .ToArray();
        var runtimeEntries = runtimeDocuments
            .SelectMany(document => loader.LoadFromJson(document.Json, document.SourceName))
            .ToArray();
        var stagingDocuments = GetStagingFiles(repoRoot)
            .Select(path => ReadDocument(repoRoot, path, GetStagingDocumentKind(path)))
            .ToArray();
        var docsExampleDocuments = GetDocsExampleFiles(repoRoot)
            .Select(path => ReadDocument(repoRoot, path, EquipmentDiagnosticsVerificationDocumentKind.DocsExample))
            .ToArray();

        return new EquipmentDiagnosticsVerificationInput(
            RuntimeEntries: runtimeEntries,
            RuntimeDocuments: runtimeDocuments,
            StagingDocuments: stagingDocuments,
            DocsExampleDocuments: docsExampleDocuments,
            KnownManualIds: GetKnownManualIds(repoRoot));
    }

    private static IReadOnlyList<string> GetRuntimeCatalogFiles(string repoRoot)
    {
        var knowledgeRoot = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Knowledge");

        return Directory.GetFiles(knowledgeRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Where(path => !HasPathSegment(path, "staging"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> GetStagingFiles(string repoRoot)
    {
        var stagingRoot = Path.Combine(
            repoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Knowledge",
            "staging");

        return Directory.GetFiles(stagingRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> GetDocsExampleFiles(string repoRoot)
    {
        var examplesRoot = Path.Combine(repoRoot, "docs", "equipment-diagnostics", "examples");

        return Directory.GetFiles(examplesRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlySet<string> GetKnownManualIds(string repoRoot)
    {
        var registryRoot = Path.Combine(repoRoot, "docs", "equipment-diagnostics", "manual-sources");
        if (!Directory.Exists(registryRoot))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(registryRoot, "*.json", SearchOption.AllDirectories))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("manualSources", out var sources) ||
                sources.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                continue;
            }

            foreach (var source in sources.EnumerateArray())
            {
                if (source.TryGetProperty("manualId", out var manualId) &&
                    manualId.ValueKind == System.Text.Json.JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(manualId.GetString()))
                {
                    ids.Add(manualId.GetString()!);
                }
            }
        }

        return ids;
    }

    private static EquipmentDiagnosticsVerificationDocumentKind GetStagingDocumentKind(string path)
    {
        if (HasPathSegment(path, "templates"))
        {
            return EquipmentDiagnosticsVerificationDocumentKind.StagingTemplate;
        }

        if (HasPathSegment(path, "examples"))
        {
            return EquipmentDiagnosticsVerificationDocumentKind.StagingExample;
        }

        return EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate;
    }

    private static EquipmentDiagnosticsVerificationDocument ReadDocument(
        string repoRoot,
        string path,
        EquipmentDiagnosticsVerificationDocumentKind kind) =>
        new(
            SourceName: Path.GetRelativePath(repoRoot, path).Replace('\\', '/'),
            Json: File.ReadAllText(path),
            Kind: kind);

    private static bool HasPathSegment(string path, string segment) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains(segment, StringComparer.OrdinalIgnoreCase);
}
