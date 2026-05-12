using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

// P3-13: extracted from BuildingInputValidationService to keep the public service as a focused facade.
// Ventilation validation extracted without changing diagnostic semantics.
public sealed partial class BuildingInputValidationService
{


    private static void ValidateVentilation(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var ventilation = room.VentilationParameters;
            var ventilationPath = $"{roomPath}.ventilationParameters";
            if (ventilation is null)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ventilation.MechanicalAndNaturalVentilationMissing",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Ventilation,
                    scope: BuildingInputValidationScope.Ventilation,
                    targetPath: ventilationPath,
                    message: "Mechanical and natural ventilation inputs are both missing."));
                continue;
            }

            ValidateVentilationParameters(ventilation, ventilationPath, diagnostics);

            var hasMechanical = ventilation.AirChangesPerHour > 0.0;
            var inferredNaturalOpeningAreaM2 = Math.Max(0.0, room.Windows.Sum(window => window.Area.SquareMeters)) * 0.25;
            if (inferredNaturalOpeningAreaM2 < 0.0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ventilation.NaturalOpeningAreaNegative",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Ventilation,
                    scope: BuildingInputValidationScope.Room,
                    targetPath: $"{roomPath}.windows",
                    message: "Inferred natural ventilation effective opening area cannot be negative."));
            }

            if (!hasMechanical && inferredNaturalOpeningAreaM2 <= 0.0)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ventilation.MechanicalAndNaturalVentilationMissing",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Ventilation,
                    scope: BuildingInputValidationScope.Room,
                    targetPath: roomPath,
                    message: "Mechanical and natural ventilation inputs are both missing."));
            }
        }
    }




    private static void ValidateVentilationParameters(
        VentilationParameters ventilation,
        string ventilationPath,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (ventilation.AirChangesPerHour < 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ventilation.AchNegative",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ventilation,
                scope: BuildingInputValidationScope.Ventilation,
                targetPath: $"{ventilationPath}.airChangesPerHour",
                message: "Ventilation ACH cannot be negative.",
                suggestedCorrection: new BuildingInputSuggestedCorrection(
                    CorrectionId: "BIV-CORR-VENT-ACH-CLAMP-ZERO",
                    TargetPath: $"{ventilationPath}.airChangesPerHour",
                    Description: "Clamp ACH to 0 for deterministic minimum safe bound.",
                    ProposedValue: "0",
                    IsAutomaticSafe: true,
                    RequiresUserReview: true)));
        }

        if (ventilation.HeatRecoveryEfficiency is < 0.0 or > 1.0 || double.IsNaN(ventilation.HeatRecoveryEfficiency))
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ventilation.HeatRecoveryEfficiencyOutOfRange",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ventilation,
                scope: BuildingInputValidationScope.Ventilation,
                targetPath: $"{ventilationPath}.heatRecoveryEfficiency",
                message: "Heat-recovery efficiency must be between 0 and 1."));
        }
    }
}