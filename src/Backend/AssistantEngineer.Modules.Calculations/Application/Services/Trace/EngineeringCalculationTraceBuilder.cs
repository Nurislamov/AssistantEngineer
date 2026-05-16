using System.Globalization;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Models.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Trace;

public sealed class EngineeringCalculationTraceBuilder : IEngineeringCalculationTraceBuilder
{
    private const double ConsistencyToleranceW = 1e-6;

    public EngineeringCalculationTrace BuildRoomHeatingTrace(RoomHeatingLoadTraceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var componentSumW =
            input.TransmissionHeatLossW +
            input.VentilationHeatLossW +
            input.InfiltrationHeatLossW +
            input.GroundHeatLossW -
            input.SolarGainW -
            input.InternalGainW;

        var mismatchW = componentSumW - input.TotalHeatingLoadW;
        var hasMismatch = Math.Abs(mismatchW) > ConsistencyToleranceW;

        var sections = new List<EngineeringCalculationTraceSection>
        {
            CreateSingleValueSection(
                sectionId: "section-transmission",
                title: "Transmission heat loss",
                category: "Transmission",
                lineId: "line-transmission-loss",
                label: "TransmissionHeatLossW",
                formula: "Q_transmission_W = sum(envelope U * A * deltaT)",
                unit: "W",
                value: input.TransmissionHeatLossW,
                explanation: "Heat loss through external envelope components."),
            CreateSingleValueSection(
                sectionId: "section-ventilation",
                title: "Ventilation heat loss",
                category: "Ventilation",
                lineId: "line-ventilation-loss",
                label: "VentilationHeatLossW",
                formula: "Q_ventilation_W = sensible ventilation heat loss",
                unit: "W",
                value: input.VentilationHeatLossW,
                explanation: "Sensible heat loss associated with ventilation airflow."),
            CreateSingleValueSection(
                sectionId: "section-infiltration",
                title: "Infiltration heat loss",
                category: "Infiltration",
                lineId: "line-infiltration-loss",
                label: "InfiltrationHeatLossW",
                formula: "Q_infiltration_W = sensible infiltration heat loss",
                unit: "W",
                value: input.InfiltrationHeatLossW,
                explanation: "Sensible heat loss associated with infiltration airflow."),
            CreateSingleValueSection(
                sectionId: "section-ground",
                title: "Ground heat loss",
                category: "Ground",
                lineId: "line-ground-loss",
                label: "GroundHeatLossW",
                formula: "Q_ground_W = A_ground * U_ground * deltaT",
                unit: "W",
                value: input.GroundHeatLossW,
                explanation: "Heat loss through ground-contact boundaries."),
            CreateSingleValueSection(
                sectionId: "section-solar",
                title: "Solar gains",
                category: "Solar",
                lineId: "line-solar-gain",
                label: "SolarGainW",
                formula: "Q_solar_W = useful solar heat gains",
                unit: "W",
                value: input.SolarGainW,
                explanation: "Solar gains reduce net heating demand."),
            CreateSingleValueSection(
                sectionId: "section-internal-gains",
                title: "Internal gains",
                category: "InternalGains",
                lineId: "line-internal-gain",
                label: "InternalGainW",
                formula: "Q_internal_W = useful internal sensible gains",
                unit: "W",
                value: input.InternalGainW,
                explanation: "Internal gains reduce net heating demand."),
            CreateFinalHeatingSection(input, componentSumW, mismatchW, hasMismatch),
            CreateAssumptionsAndExclusionsSection(input.Assumptions, input.ExcludedEffects),
            CreateDiagnosticsSection(input.DiagnosticReferences)
        };

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["componentSumW"] = componentSumW.ToString("G17", CultureInfo.InvariantCulture),
            ["totalHeatingLoadW"] = input.TotalHeatingLoadW.ToString("G17", CultureInfo.InvariantCulture),
            ["consistencyToleranceW"] = ConsistencyToleranceW.ToString("G17", CultureInfo.InvariantCulture),
            ["consistencyStatus"] = hasMismatch ? "MismatchWarning" : "Consistent"
        };

        if (hasMismatch)
        {
            metadata["componentMismatchW"] = mismatchW.ToString("G17", CultureInfo.InvariantCulture);
            metadata["consistencyWarningCode"] = "TRACE-CONSISTENCY-001";
        }

        return new EngineeringCalculationTrace(
            TraceId: CreateTraceId(input.RoomId),
            Scope: "RoomHeatingExplainability",
            SubjectType: "Room",
            SubjectId: input.RoomId,
            CalculationType: "RoomHeatingLoad",
            Sections: sections,
            Assumptions: input.Assumptions ?? [],
            ExcludedEffects: input.ExcludedEffects ?? [],
            DiagnosticReferences: input.DiagnosticReferences ?? [],
            Metadata: metadata);
    }

    private static EngineeringCalculationTraceSection CreateFinalHeatingSection(
        RoomHeatingLoadTraceInput input,
        double componentSumW,
        double mismatchW,
        bool hasMismatch)
    {
        var lines = new List<EngineeringCalculationTraceLine>
        {
            new(
                LineId: "line-final-heating-load",
                Label: "TotalHeatingLoadW",
                Formula: "Q_total_W = Q_transmission + Q_ventilation + Q_infiltration + Q_ground - Q_solar - Q_internal",
                Inputs: new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["TransmissionHeatLossW"] = input.TransmissionHeatLossW,
                    ["VentilationHeatLossW"] = input.VentilationHeatLossW,
                    ["InfiltrationHeatLossW"] = input.InfiltrationHeatLossW,
                    ["GroundHeatLossW"] = input.GroundHeatLossW,
                    ["SolarGainW"] = input.SolarGainW,
                    ["InternalGainW"] = input.InternalGainW
                },
                Unit: "W",
                Value: input.TotalHeatingLoadW,
                Explanation: "Final reported heating load from calculation output.",
                Source: "RoomHeatingLoadResult")
        };

        if (hasMismatch)
        {
            lines.Add(new EngineeringCalculationTraceLine(
                LineId: "line-consistency-warning",
                Label: "ComponentSumMismatchW",
                Formula: "Mismatch = componentSumW - TotalHeatingLoadW",
                Inputs: new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["componentSumW"] = componentSumW,
                    ["totalHeatingLoadW"] = input.TotalHeatingLoadW
                },
                Unit: "W",
                Value: mismatchW,
                Explanation: "Trace arithmetic consistency warning. This does not modify or override calculation output.",
                Source: "EngineeringCalculationTraceBuilder",
                Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["warningCode"] = "TRACE-CONSISTENCY-001",
                    ["severity"] = "Warning"
                }));
        }

        return new EngineeringCalculationTraceSection(
            SectionId: "section-final-heating-load",
            Title: "Final heating load",
            Category: "CalculationResult",
            Lines: lines,
            Metadata: hasMismatch
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["consistencyStatus"] = "MismatchWarning"
                }
                : new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["consistencyStatus"] = "Consistent"
                });
    }

    private static EngineeringCalculationTraceSection CreateAssumptionsAndExclusionsSection(
        IReadOnlyList<EngineeringCalculationTraceAssumption>? assumptions,
        IReadOnlyList<EngineeringCalculationTraceExcludedEffect>? excludedEffects)
    {
        var lines = new List<EngineeringCalculationTraceLine>();

        if (assumptions is not null)
        {
            lines.AddRange(assumptions
                .OrderBy(assumption => assumption.AssumptionId, StringComparer.Ordinal)
                .Select(assumption => new EngineeringCalculationTraceLine(
                    LineId: $"line-assumption-{assumption.AssumptionId}",
                    Label: assumption.Name,
                    Formula: null,
                    Inputs: null,
                    Unit: assumption.Unit,
                    Value: TryParseInvariantDouble(assumption.Value),
                    Explanation: $"{assumption.Status} assumption. Source: {assumption.Source}.",
                    Source: assumption.RegistryReference)));
        }

        if (excludedEffects is not null)
        {
            lines.AddRange(excludedEffects
                .OrderBy(effect => effect.Effect, StringComparer.Ordinal)
                .Select(effect => new EngineeringCalculationTraceLine(
                    LineId: $"line-excluded-{NormalizeId(effect.Effect)}",
                    Label: $"Excluded: {effect.Effect}",
                    Formula: null,
                    Inputs: null,
                    Unit: null,
                    Value: null,
                    Explanation: effect.Reason,
                    Source: effect.Source)));
        }

        return new EngineeringCalculationTraceSection(
            SectionId: "section-assumptions-exclusions",
            Title: "Assumptions and exclusions",
            Category: "Assumptions",
            Lines: lines);
    }

    private static EngineeringCalculationTraceSection CreateDiagnosticsSection(
        IReadOnlyList<EngineeringCalculationTraceDiagnosticReference>? diagnostics)
    {
        var lines = diagnostics is null
            ? []
            : diagnostics
                .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .Select(diagnostic => new EngineeringCalculationTraceLine(
                    LineId: $"line-diagnostic-{diagnostic.Code}",
                    Label: diagnostic.Code,
                    Formula: null,
                    Inputs: null,
                    Unit: null,
                    Value: null,
                    Explanation: $"{diagnostic.Severity} [{diagnostic.Category}] {diagnostic.Message}",
                    Source: "Diagnostics"))
                .ToArray();

        return new EngineeringCalculationTraceSection(
            SectionId: "section-diagnostics",
            Title: "Diagnostics",
            Category: "Diagnostics",
            Lines: lines);
    }

    private static EngineeringCalculationTraceSection CreateSingleValueSection(
        string sectionId,
        string title,
        string category,
        string lineId,
        string label,
        string formula,
        string unit,
        double value,
        string explanation)
    {
        return new EngineeringCalculationTraceSection(
            SectionId: sectionId,
            Title: title,
            Category: category,
            Lines:
            [
                new EngineeringCalculationTraceLine(
                    LineId: lineId,
                    Label: label,
                    Formula: formula,
                    Inputs: null,
                    Unit: unit,
                    Value: value,
                    Explanation: explanation,
                    Source: "CalculationComponents")
            ]);
    }

    private static string CreateTraceId(int? roomId)
    {
        var suffix = roomId.HasValue
            ? $"room-{roomId.Value.ToString(CultureInfo.InvariantCulture)}"
            : "room-unknown";

        return $"trace-room-heating-{suffix}-{Guid.NewGuid():N}";
    }

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        return string.Join(string.Empty, chars).Trim('-');
    }

    private static double? TryParseInvariantDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
