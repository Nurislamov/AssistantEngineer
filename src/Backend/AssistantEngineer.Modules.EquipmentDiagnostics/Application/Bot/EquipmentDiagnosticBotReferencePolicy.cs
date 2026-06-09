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

    public static bool IsReferenceOnlyCode(string normalizedCode) =>
        ReferenceOnlyCodes.Contains(normalizedCode);

    public static bool IsControllerModelName(string normalizedCode) =>
        ControllerModelNames.Contains(normalizedCode);
}
