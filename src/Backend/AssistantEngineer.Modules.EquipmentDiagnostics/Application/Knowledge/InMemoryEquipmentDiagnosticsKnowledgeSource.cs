using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

public sealed class InMemoryEquipmentDiagnosticsKnowledgeSource : IEquipmentDiagnosticsKnowledgeSource
{
    private readonly EquipmentDiagnosticsJsonKnowledgeSource _source = new();

    public IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetEntries() =>
        _source.GetEntries();
}
