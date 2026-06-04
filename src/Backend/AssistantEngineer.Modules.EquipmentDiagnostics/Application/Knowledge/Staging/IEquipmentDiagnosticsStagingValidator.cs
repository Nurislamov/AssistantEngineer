namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;

public interface IEquipmentDiagnosticsStagingValidator
{
    EquipmentDiagnosticsStagingValidationResult ValidateJson(
        string json,
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> productionEntries,
        string sourceName = "staging-candidates.json");

    EquipmentDiagnosticsStagingValidationResult ValidateCandidates(
        IReadOnlyCollection<EquipmentDiagnosticsStagingCandidate> candidates,
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> productionEntries,
        string sourceName = "staging-candidates");
}
