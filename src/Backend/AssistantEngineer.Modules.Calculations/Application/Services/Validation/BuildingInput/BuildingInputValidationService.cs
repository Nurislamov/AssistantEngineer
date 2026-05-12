using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

public sealed partial class BuildingInputValidationService
{
    private static readonly IReadOnlyList<string> ClaimBoundary =
    [
        "Building input validation and correction framework.",
        "Internal deterministic engineering governance only.",
        "No automatic production data mutation.",
        "No full ISO/EN compliance claim.",
        "No StandardReference equivalence claim.",
        "No EnergyPlus comparison workflow claim.",
        "No ASHRAE 140 / BESTEST-style validation anchor claim.",
        "No external certification claim."
    ];

    private readonly Iso52016ConstructionOptions _constructionOptions;

    public BuildingInputValidationService(
        IOptions<Iso52016ConstructionOptions>? constructionOptions = null)
    {
        _constructionOptions = constructionOptions?.Value ?? new Iso52016ConstructionOptions();
    }

    public BuildingInputValidationResult Validate(Building building) =>
        Validate(new BuildingInputValidationRequest(building));

    public BuildingInputValidationResult Validate(BuildingInputValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<BuildingInputValidationDiagnostic>();
        if (request.Building is null)
        {
            diagnostics.Add(CreateDiagnostic(
                code: "BuildingInput.BuildingRequired",
                severity: BuildingInputValidationSeverity.Critical,
                category: BuildingInputValidationCategory.DataCompleteness,
                scope: BuildingInputValidationScope.Building,
                targetPath: "$.building",
                message: "Building input is required."));
            return BuildResult(diagnostics, request.EvaluateIso52016Readiness);
        }

        ValidateGeometry(request.Building, diagnostics);
        ValidateEnvelope(request.Building, diagnostics);
        ValidateOpenings(request.Building, diagnostics);
        ValidateVentilation(request.Building, diagnostics);
        ValidateGround(request.Building, diagnostics);
        ValidateConstruction(request.Building, request, diagnostics);
        ValidateDomesticHotWater(request, diagnostics);
        ValidateSystemEnergy(request, diagnostics);

        if (request.EvaluateIso52016Readiness)
            ValidateIso52016Readiness(request.Building, diagnostics);

        return BuildResult(diagnostics, request.EvaluateIso52016Readiness);
    }}