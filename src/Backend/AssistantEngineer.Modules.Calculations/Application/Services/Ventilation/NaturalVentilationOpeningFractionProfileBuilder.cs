using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationOpeningFractionProfileBuilder : INaturalVentilationOpeningFractionProfileBuilder
{
    private readonly INaturalVentilationOpeningControlEvaluator _controlEvaluator;

    public NaturalVentilationOpeningFractionProfileBuilder(
        INaturalVentilationOpeningControlEvaluator controlEvaluator)
    {
        _controlEvaluator = controlEvaluator ?? throw new ArgumentNullException(nameof(controlEvaluator));
    }

    public NaturalVentilationControlEvaluationResult BuildProfiles(
        NaturalVentilationControlEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.HourlyContexts);

        var evaluated = _controlEvaluator.Evaluate(input);
        var diagnostics = new List<StandardCalculationDiagnostic>(evaluated.Diagnostics);
        var operations = evaluated.Operations;

        var hourIndexes = input.HourlyContexts
            .Select(context => context.HourIndex)
            .Distinct()
            .OrderBy(hour => hour)
            .ToArray();

        var targetProfileLength = hourIndexes.Length;
        if (hourIndexes.Length == 0)
        {
            targetProfileLength = 0;
        }
        else if (hourIndexes.Length == 24 || hourIndexes.Length == 8760)
        {
            targetProfileLength = hourIndexes.Length;
        }
        else
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-CONTROL-PROFILE-LENGTH-NONSTANDARD",
                $"Natural ventilation control profiles use nonstandard hour count {hourIndexes.Length}."));
        }

        var openingProfiles = BuildGroupedProfiles(
            operations.Where(operation => !string.IsNullOrWhiteSpace(operation.OpeningId)),
            operation => operation.OpeningId!,
            hourIndexes,
            targetProfileLength,
            diagnostics,
            "opening");

        if (operations.Any(operation => string.IsNullOrWhiteSpace(operation.OpeningId)))
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-CONTROL-OPENING-ID-MISSING-FOR-PROFILE",
                "One or more control operations cannot contribute to opening profiles because OpeningId is missing."));
        }

        var roomProfiles = BuildGroupedProfiles(
            operations.Where(operation => !string.IsNullOrWhiteSpace(operation.RoomId)),
            operation => operation.RoomId!,
            hourIndexes,
            targetProfileLength,
            diagnostics,
            "room");

        var zoneProfiles = BuildGroupedProfiles(
            operations.Where(operation => !string.IsNullOrWhiteSpace(operation.ZoneId)),
            operation => operation.ZoneId!,
            hourIndexes,
            targetProfileLength,
            diagnostics,
            "zone");

        diagnostics.Add(CreateInfo(
            "AE-VENT-CONTROL-PROFILE-BUILT",
            $"Natural ventilation control profiles built for {openingProfiles.Count} opening(s), {roomProfiles.Count} room(s), and {zoneProfiles.Count} zone(s)."));

        return new NaturalVentilationControlEvaluationResult(
            Operations: operations,
            OpeningFractionProfilesByOpeningId: openingProfiles,
            RoomOpeningFractionProfilesByRoomId: roomProfiles,
            ZoneOpeningFractionProfilesByZoneId: zoneProfiles,
            Disclosure: evaluated.Disclosure,
            Diagnostics: diagnostics);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<double>> BuildGroupedProfiles(
        IEnumerable<NaturalVentilationOpeningOperationResult> operations,
        Func<NaturalVentilationOpeningOperationResult, string> keySelector,
        IReadOnlyList<int> hourIndexes,
        int targetProfileLength,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        string label)
    {
        var profileByKey = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);

        foreach (var group in operations.GroupBy(keySelector, StringComparer.Ordinal))
        {
            var profile = new List<double>();
            var operationsByHour = group
                .GroupBy(operation => operation.HourIndex)
                .ToDictionary(
                    hourGroup => hourGroup.Key,
                    hourGroup =>
                    {
                        if (hourGroup.Count() > 1)
                        {
                            diagnostics.Add(CreateInfo(
                                "AE-VENT-CONTROL-MULTIPLE-RULES-MAX-FRACTION-USED",
                                $"Multiple control rules applied to the same {label} '{group.Key}' at hour {hourGroup.Key}; maximum opening fraction was used."));
                        }

                        return hourGroup.Max(operation => operation.OpeningFraction);
                    });

            if (targetProfileLength == 8760)
            {
                var hasOutOfRangeHours = operationsByHour.Keys.Any(hour => hour is < 0 or > 8759);
                var missingHourCount = 0;

                for (var hour = 0; hour < 8760; hour++)
                {
                    if (operationsByHour.TryGetValue(hour, out var value))
                    {
                        profile.Add(value);
                    }
                    else
                    {
                        profile.Add(0.0);
                        missingHourCount++;
                    }
                }

                if (missingHourCount > 0 || hasOutOfRangeHours)
                {
                    diagnostics.Add(CreateInfo(
                        "AE-VENT-CONTROL-HOURLY-GAPS-FILLED",
                        $"Profile gaps were filled with zeros for {label} '{group.Key}' when building 8760-hour profile."));
                }
            }
            else
            {
                foreach (var hourIndex in hourIndexes)
                {
                    profile.Add(operationsByHour.TryGetValue(hourIndex, out var value) ? value : 0.0);
                }
            }

            profileByKey[group.Key] = profile;
        }

        return profileByKey;
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "NaturalVentilationOpeningFractionProfileBuilder");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "NaturalVentilationOpeningFractionProfileBuilder");
}
