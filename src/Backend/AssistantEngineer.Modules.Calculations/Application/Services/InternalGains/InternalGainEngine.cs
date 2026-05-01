using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;

public sealed class InternalGainEngine
{
    public Result<InternalGainResult> Calculate(
        InternalGainInput input)
    {
        if (input is null)
            return Result<InternalGainResult>.Validation("Internal gain input is required.");

        var diagnostics = Validate(input);

        if (diagnostics.Any(diagnostic =>
                diagnostic.Severity == CalculationDiagnosticSeverity.Error))
        {
            return Result<InternalGainResult>.Success(
                Empty(input, diagnostics));
        }

        var occupancySensibleGainW =
            ResolveNonNegative(input.OccupancyPeople) *
            ResolveNonNegative(input.SensibleGainPerPersonW) *
            input.OccupancyScheduleFactor;

        var occupancyLatentGainW =
            ResolveNonNegative(input.OccupancyPeople) *
            ResolveNonNegative(input.LatentGainPerPersonW) *
            input.OccupancyScheduleFactor;

        var lightingGainW =
            ResolveExplicitAndAreaBasedGain(
                input.LightingLoadW,
                input.LightingPowerDensityWPerM2,
                input.AreaM2) *
            input.LightingScheduleFactor;

        var equipmentGainW =
            ResolveExplicitAndAreaBasedGain(
                input.EquipmentLoadW,
                input.EquipmentPowerDensityWPerM2,
                input.AreaM2) *
            input.EquipmentScheduleFactor;

        var processSensibleGainW =
            ResolveNonNegative(input.ProcessSensibleGainW) *
            input.ProcessScheduleFactor;

        var processLatentGainW =
            ResolveNonNegative(input.ProcessLatentGainW) *
            input.ProcessScheduleFactor;

        var customSensibleGainW =
            ResolveNonNegative(input.CustomSensibleGainW) *
            input.CustomScheduleFactor;

        var customLatentGainW =
            ResolveNonNegative(input.CustomLatentGainW) *
            input.CustomScheduleFactor;

        var totalSensibleGainW =
            occupancySensibleGainW +
            lightingGainW +
            equipmentGainW +
            processSensibleGainW +
            customSensibleGainW;

        var totalLatentGainW =
            occupancyLatentGainW +
            processLatentGainW +
            customLatentGainW;

        var totalInternalGainW =
            totalSensibleGainW + totalLatentGainW;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "InternalGains.Calculated",
            "Internal gains were calculated from occupancy, lighting, equipment, process and custom gain components.",
            input.DiagnosticsContext));

        if (totalLatentGainW > 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "InternalGains.LatentGainCalculatedButNotUsedByIso52016SensiblePath",
                "Latent internal gains were calculated, but the current ISO 52016 room heat-balance path consumes sensible internal gains only.",
                input.DiagnosticsContext));
        }

        return Result<InternalGainResult>.Success(
            new InternalGainResult(
                RoomId: input.RoomId,

                OccupancySensibleGainW: Round(occupancySensibleGainW),
                OccupancyLatentGainW: Round(occupancyLatentGainW),

                LightingGainW: Round(lightingGainW),
                EquipmentGainW: Round(equipmentGainW),

                ProcessSensibleGainW: Round(processSensibleGainW),
                ProcessLatentGainW: Round(processLatentGainW),

                CustomSensibleGainW: Round(customSensibleGainW),
                CustomLatentGainW: Round(customLatentGainW),

                TotalSensibleGainW: Round(totalSensibleGainW),
                TotalLatentGainW: Round(totalLatentGainW),
                TotalInternalGainW: Round(totalInternalGainW),

                AreaM2: input.AreaM2,
                OccupancyPeople: input.OccupancyPeople,

                OccupancyScheduleFactor: input.OccupancyScheduleFactor,
                LightingScheduleFactor: input.LightingScheduleFactor,
                EquipmentScheduleFactor: input.EquipmentScheduleFactor,
                ProcessScheduleFactor: input.ProcessScheduleFactor,
                CustomScheduleFactor: input.CustomScheduleFactor,

                Diagnostics: diagnostics));
    }

    private static InternalGainResult Empty(
        InternalGainInput input,
        IReadOnlyList<CalculationDiagnostic> diagnostics) =>
        new(
            RoomId: input.RoomId,

            OccupancySensibleGainW: 0,
            OccupancyLatentGainW: 0,

            LightingGainW: 0,
            EquipmentGainW: 0,

            ProcessSensibleGainW: 0,
            ProcessLatentGainW: 0,

            CustomSensibleGainW: 0,
            CustomLatentGainW: 0,

            TotalSensibleGainW: 0,
            TotalLatentGainW: 0,
            TotalInternalGainW: 0,

            AreaM2: input.AreaM2,
            OccupancyPeople: input.OccupancyPeople,

            OccupancyScheduleFactor: input.OccupancyScheduleFactor,
            LightingScheduleFactor: input.LightingScheduleFactor,
            EquipmentScheduleFactor: input.EquipmentScheduleFactor,
            ProcessScheduleFactor: input.ProcessScheduleFactor,
            CustomScheduleFactor: input.CustomScheduleFactor,

            Diagnostics: diagnostics);

    private static List<CalculationDiagnostic> Validate(
        InternalGainInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (input.RoomId < 0)
        {
            diagnostics.Add(Error(
                "InternalGains.InvalidRoomId",
                "Room id must not be negative.",
                input.DiagnosticsContext));
        }

        if (input.AreaM2.HasValue && input.AreaM2.Value <= 0)
        {
            diagnostics.Add(Error(
                "InternalGains.InvalidArea",
                "Room area must be greater than zero when supplied.",
                input.DiagnosticsContext));
        }

        if (input.OccupancyPeople.HasValue && input.OccupancyPeople.Value < 0)
        {
            diagnostics.Add(Error(
                "InternalGains.InvalidOccupancy",
                "Occupancy people count must not be negative.",
                input.DiagnosticsContext));
        }

        ValidateNonNegative(
            diagnostics,
            input.SensibleGainPerPersonW,
            "InternalGains.InvalidSensibleGainPerPerson",
            "Sensible gain per person must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.LatentGainPerPersonW,
            "InternalGains.InvalidLatentGainPerPerson",
            "Latent gain per person must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.LightingLoadW,
            "InternalGains.InvalidLightingLoad",
            "Lighting load must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.LightingPowerDensityWPerM2,
            "InternalGains.InvalidLightingPowerDensity",
            "Lighting power density must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.EquipmentLoadW,
            "InternalGains.InvalidEquipmentLoad",
            "Equipment load must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.EquipmentPowerDensityWPerM2,
            "InternalGains.InvalidEquipmentPowerDensity",
            "Equipment power density must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.ProcessSensibleGainW,
            "InternalGains.InvalidProcessSensibleGain",
            "Process sensible gain must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.ProcessLatentGainW,
            "InternalGains.InvalidProcessLatentGain",
            "Process latent gain must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.CustomSensibleGainW,
            "InternalGains.InvalidCustomSensibleGain",
            "Custom sensible gain must not be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.CustomLatentGainW,
            "InternalGains.InvalidCustomLatentGain",
            "Custom latent gain must not be negative.",
            input.DiagnosticsContext);

        ValidateScheduleFactor(
            diagnostics,
            input.OccupancyScheduleFactor,
            "InternalGains.InvalidOccupancyScheduleFactor",
            "Occupancy schedule factor must be between 0 and 1.",
            input.DiagnosticsContext);

        ValidateScheduleFactor(
            diagnostics,
            input.LightingScheduleFactor,
            "InternalGains.InvalidLightingScheduleFactor",
            "Lighting schedule factor must be between 0 and 1.",
            input.DiagnosticsContext);

        ValidateScheduleFactor(
            diagnostics,
            input.EquipmentScheduleFactor,
            "InternalGains.InvalidEquipmentScheduleFactor",
            "Equipment schedule factor must be between 0 and 1.",
            input.DiagnosticsContext);

        ValidateScheduleFactor(
            diagnostics,
            input.ProcessScheduleFactor,
            "InternalGains.InvalidProcessScheduleFactor",
            "Process schedule factor must be between 0 and 1.",
            input.DiagnosticsContext);

        ValidateScheduleFactor(
            diagnostics,
            input.CustomScheduleFactor,
            "InternalGains.InvalidCustomScheduleFactor",
            "Custom schedule factor must be between 0 and 1.",
            input.DiagnosticsContext);

        ValidateAreaBasedGainHasArea(
            diagnostics,
            input.AreaM2,
            input.LightingPowerDensityWPerM2,
            "InternalGains.MissingAreaForLightingPowerDensity",
            "Room area is required when lighting power density is supplied.",
            input.DiagnosticsContext);

        ValidateAreaBasedGainHasArea(
            diagnostics,
            input.AreaM2,
            input.EquipmentPowerDensityWPerM2,
            "InternalGains.MissingAreaForEquipmentPowerDensity",
            "Room area is required when equipment power density is supplied.",
            input.DiagnosticsContext);

        return diagnostics;
    }

    private static void ValidateNonNegative(
        ICollection<CalculationDiagnostic> diagnostics,
        double? value,
        string code,
        string message,
        string? context)
    {
        if (value is < 0)
            diagnostics.Add(Error(code, message, context));
    }

    private static void ValidateScheduleFactor(
        ICollection<CalculationDiagnostic> diagnostics,
        double value,
        string code,
        string message,
        string? context)
    {
        if (value is < 0.0 or > 1.0)
            diagnostics.Add(Error(code, message, context));
    }

    private static void ValidateAreaBasedGainHasArea(
        ICollection<CalculationDiagnostic> diagnostics,
        double? areaM2,
        double? powerDensityWPerM2,
        string code,
        string message,
        string? context)
    {
        if (powerDensityWPerM2.HasValue &&
            powerDensityWPerM2.Value > 0 &&
            !areaM2.HasValue)
        {
            diagnostics.Add(Error(code, message, context));
        }
    }

    private static double ResolveExplicitAndAreaBasedGain(
        double? explicitGainW,
        double? powerDensityWPerM2,
        double? areaM2)
    {
        var gain = ResolveNonNegative(explicitGainW);

        if (powerDensityWPerM2.HasValue && areaM2.HasValue)
            gain += ResolveNonNegative(powerDensityWPerM2) * areaM2.Value;

        return gain;
    }

    private static double ResolveNonNegative(
        double? value) =>
        value.HasValue && value.Value > 0 ? value.Value : 0.0;

    private static CalculationDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            context);

    private static double Round(
        double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}