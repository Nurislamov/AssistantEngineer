namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

public sealed class InMemoryEquipmentDiagnosticsKnowledgeSource : IEquipmentDiagnosticsKnowledgeSource
{
    public IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetEntries() =>
        EquipmentDiagnosticsKnowledgeCatalog.Entries;
}
