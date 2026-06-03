namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

public interface IEquipmentDiagnosticsKnowledgeSource
{
    IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetEntries();
}
