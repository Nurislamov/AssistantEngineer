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
// Ground/boundary/construction validation extracted without changing diagnostic semantics.
public sealed partial class BuildingInputValidationService
{


    private static void ValidateGround(
        Building building,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var hasGroundBoundary = room.Walls.Any(wall => wall.BoundaryType == WallBoundaryType.Ground);
            if (!hasGroundBoundary)
                continue;

            if (room.GroundContactMetadata is null)
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Ground.MetadataMissingForGroundBoundary",
                    severity: BuildingInputValidationSeverity.Warning,
                    category: BuildingInputValidationCategory.Ground,
                    scope: BuildingInputValidationScope.Ground,
                    targetPath: $"{roomPath}.groundContactMetadata",
                    message: "Ground-contact metadata is missing for room with ground boundary; fallback assumptions may be used.",
                    suggestedCorrection: new BuildingInputSuggestedCorrection(
                        CorrectionId: "BIV-CORR-GROUND-METADATA-ADD",
                        TargetPath: $"{roomPath}.groundContactMetadata",
                        Description: "Provide deterministic ground-contact metadata (contact type, exposed perimeter, burial depth).",
                        ProposedValue: null,
                        IsAutomaticSafe: false,
                        RequiresUserReview: true)));
                continue;
            }

            ValidateGroundMetadata(room, room.GroundContactMetadata, roomPath, diagnostics);
        }
    }




    private void ValidateConstruction(
        Building building,
        BuildingInputValidationRequest request,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        var constructionOptInIntended =
            request.IsConstructionLayerMassOptInIntended ||
            _constructionOptions.UseConstructionLayerMassInput;

        foreach (var (room, roomPath) in EnumerateRooms(building))
        {
            var heatTransferWalls = room.Walls.Where(IsHeatTransferWall).ToArray();
            foreach (var (wall, wallIndex) in heatTransferWalls.Select((item, index) => (item, index)))
            {
                var wallPath = $"{roomPath}.walls[{wallIndex}]";
                ValidateConstructionAssembly(wall.ConstructionAssembly, wallPath, diagnostics);
            }

            if (!constructionOptInIntended)
                continue;

            if (heatTransferWalls.Length == 0)
                continue;

            var wallsWithoutLayers = heatTransferWalls
                .Where(wall => wall.ConstructionAssembly is null || wall.ConstructionAssembly.Layers.Count == 0)
                .ToArray();
            if (wallsWithoutLayers.Length == 0)
                continue;

            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Construction.OptInMissingLayersFallbackWillBeUsed",
                severity: BuildingInputValidationSeverity.Warning,
                category: BuildingInputValidationCategory.Construction,
                scope: BuildingInputValidationScope.Construction,
                targetPath: $"{roomPath}.walls",
                message: "Construction layer/mass opt-in is intended, but one or more heat-transfer walls have no construction layers. Equivalent fallback assemblies will be used.",
                suggestedCorrection: new BuildingInputSuggestedCorrection(
                    CorrectionId: "BIV-CORR-CONSTRUCTION-LAYERS-ADD",
                    TargetPath: $"{roomPath}.walls",
                    Description: "Add explicit construction layers or accept equivalent fallback assembly behavior for this scenario.",
                    ProposedValue: "EquivalentFallbackAssembly",
                    IsAutomaticSafe: false,
                    RequiresUserReview: true)));
        }
    }




    private static void ValidateGroundMetadata(
        Room room,
        GroundContactMetadata metadata,
        string roomPath,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (!(room.Area.SquareMeters > 0.0))
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.RoomAreaNonPositiveForGroundContact",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.area",
                message: "Ground-contact room area must be greater than zero."));
        }

        if (metadata.ExposedPerimeterM < 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.ExposedPerimeterNegative",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.groundContactMetadata.exposedPerimeterM",
                message: "Ground-contact exposed perimeter cannot be negative."));
        }

        if (metadata.BurialDepthM < 0.0)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.BurialDepthNegative",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.groundContactMetadata.burialDepthM",
                message: "Ground-contact burial depth cannot be negative."));
        }

        if (!Enum.IsDefined(metadata.ContactType))
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.Ground.ContactTypeInvalid",
                severity: BuildingInputValidationSeverity.Error,
                category: BuildingInputValidationCategory.Ground,
                scope: BuildingInputValidationScope.Ground,
                targetPath: $"{roomPath}.groundContactMetadata.contactType",
                message: "Ground-contact type must be a defined value."));
        }
    }




    private static void ValidateConstructionAssembly(
        ConstructionAssembly? assembly,
        string wallPath,
        ICollection<BuildingInputValidationDiagnostic> diagnostics)
    {
        if (assembly is null)
            return;

        foreach (var (layer, layerIndex) in assembly.Layers.Select((item, index) => (item, index)))
        {
            var layerPath = $"{wallPath}.constructionAssembly.layers[{layerIndex}]";
            if (!(layer.ThicknessM > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerThicknessNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.thicknessM",
                    message: "Construction layer thickness must be greater than zero."));
            }

            if (!(layer.Material.ThermalConductivityWPerMK > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerConductivityNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.material.thermalConductivityWPerMK",
                    message: "Construction layer conductivity must be greater than zero."));
            }

            if (!(layer.Material.DensityKgPerM3 > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerDensityNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.material.densityKgPerM3",
                    message: "Construction layer density must be greater than zero."));
            }

            if (!(layer.Material.SpecificHeatJPerKgK > 0.0))
            {
                diagnostics.Add(CreateDiagnostic(
                    code: "BuildingInput.Construction.LayerSpecificHeatNonPositive",
                    severity: BuildingInputValidationSeverity.Error,
                    category: BuildingInputValidationCategory.Construction,
                    scope: BuildingInputValidationScope.Construction,
                    targetPath: $"{layerPath}.material.specificHeatJPerKgK",
                    message: "Construction layer specific heat must be greater than zero."));
            }
        }
    }
}