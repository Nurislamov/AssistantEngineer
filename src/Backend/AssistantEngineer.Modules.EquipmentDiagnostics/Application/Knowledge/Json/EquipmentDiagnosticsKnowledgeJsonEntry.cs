namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;

public sealed class EquipmentDiagnosticsKnowledgeJsonEntry
{
    public string? Manufacturer { get; set; }

    public string? SeriesName { get; set; }

    public string? ModelCode { get; set; }

    public string? Category { get; set; }

    public string? Code { get; set; }

    public string? Title { get; set; }

    public string? Meaning { get; set; }

    public string? Severity { get; set; }

    public string? Confidence { get; set; }

    public List<string>? LikelyCauses { get; set; }

    public List<EquipmentDiagnosticsKnowledgeJsonStep>? DiagnosticSteps { get; set; }

    public List<EquipmentDiagnosticsKnowledgeJsonMeasurement>? RequiredMeasurements { get; set; }

    public List<string>? SafetyNotes { get; set; }

    public List<EquipmentDiagnosticsKnowledgeJsonManualReference>? ManualReferences { get; set; }

    public List<string>? Tags { get; set; }
}

public sealed class EquipmentDiagnosticsKnowledgeJsonStep
{
    public int Order { get; set; }

    public string? Title { get; set; }

    public string? Instruction { get; set; }

    public string? ExpectedResult { get; set; }

    public string? IfFailedAction { get; set; }
}

public sealed class EquipmentDiagnosticsKnowledgeJsonMeasurement
{
    public string? Name { get; set; }

    public string? Unit { get; set; }

    public string? Description { get; set; }

    public bool RequiredBeforeConclusion { get; set; }
}

public sealed class EquipmentDiagnosticsKnowledgeJsonManualReference
{
    public string? Manufacturer { get; set; }

    public string? ManualTitle { get; set; }

    public string? ManualVersion { get; set; }

    public string? Page { get; set; }

    public string? Notes { get; set; }
}
