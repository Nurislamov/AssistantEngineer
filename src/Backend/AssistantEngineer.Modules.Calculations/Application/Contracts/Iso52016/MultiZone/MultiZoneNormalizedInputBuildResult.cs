using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneNormalizedInputBuildResult(
    MultiZoneCalculationInput Input,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics)
{
    public bool IsValid =>
        Diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error);
}
