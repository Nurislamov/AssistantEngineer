using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public static class EquipmentDiagnosticBotReferencePolicy
{
    private static readonly HashSet<string> ReferenceOnlyCodes = new(StringComparer.Ordinal)
    {
        "A0", "A2", "A3", "A4", "A6", "A7", "A8", "A9", "AH", "AC", "AL", "AE", "AF", "AJ", "AP", "AU", "AB", "AD", "AN", "AY",
        "N0", "N1", "N3", "N4", "N5", "N6", "N7", "N8", "N9", "NA", "NH", "NC", "NE", "NF", "NJ", "NU", "NB", "NN",
        "QA", "QH", "QC", "QP", "QU", "DB",
        "C00", "C01", "C03", "C05", "P10", "P11", "P13", "P14", "P30", "P31"
    };

    private static readonly HashSet<string> ControllerModelNames = new(StringComparer.Ordinal)
    {
        "CE41", "CE42", "CE52"
    };

    private static readonly HashSet<string> QualityApprovedLocalizedEntryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "gree-gmv6-debugging-c0",
        "gree-gmv6-debugging-u0",
        "gree-gmv6-debugging-u3",
        "gree-gmv6-indoor-d1",
        "gree-gmv6-indoor-l1",
        "gree-gmv6-indoor-o1",
        "gree-gmv6-outdoor-e1",
        "gree-gmv6-outdoor-h5",
        "gree-gmv6-status-a0",
        "gree-gmv-mini-indoor-aj",
        "gree-gmv-mini-indoor-c0"
    };

    public static bool IsReferenceOnlyCode(string normalizedCode) =>
        ReferenceOnlyCodes.Contains(normalizedCode);

    public static bool IsControllerModelName(string normalizedCode) =>
        ControllerModelNames.Contains(normalizedCode);

    public static bool IsSearchableLocalizedEntry(ErrorKnowledgeEntryV2 entry) =>
        entry.SignalType is
            ErrorKnowledgeSignalType.Status or
            ErrorKnowledgeSignalType.Debug or
            ErrorKnowledgeSignalType.Commissioning or
            ErrorKnowledgeSignalType.Maintenance or
            ErrorKnowledgeSignalType.RemoteDisplay ||
        entry.PackageId.Contains("debugging", StringComparison.OrdinalIgnoreCase) ||
        entry.PackageId.Contains("status", StringComparison.OrdinalIgnoreCase) ||
        QualityApprovedLocalizedEntryIds.Contains(entry.Id);
}
