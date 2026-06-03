namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

public static class EquipmentDiagnosticsKnowledgeCatalog
{
    public const string ResourcePathFragment = ".Knowledge.";

    public static bool IsKnowledgeJsonResource(string resourceName) =>
        resourceName.Contains(ResourcePathFragment, StringComparison.Ordinal) &&
        resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
        !resourceName.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase);
}
