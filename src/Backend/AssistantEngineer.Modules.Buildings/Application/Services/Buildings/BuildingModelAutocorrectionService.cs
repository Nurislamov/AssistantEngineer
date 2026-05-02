using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class BuildingModelAutocorrectionService
{
    private readonly IBuildingRepository _buildings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BuildingAutocorrectionPlanner _planner;
    private readonly BuildingModelValidationService _validation;

    public BuildingModelAutocorrectionService(
        IBuildingRepository buildings,
        IUnitOfWork unitOfWork,
        BuildingAutocorrectionPlanner planner,
        BuildingModelValidationService validation)
    {
        _buildings = buildings;
        _unitOfWork = unitOfWork;
        _planner = planner;
        _validation = validation;
    }
    
    public async Task<Result<BuildingAutocorrectionPreview>> PreviewAsync(
        int buildingId,
        int weatherYear,
        AutocorrectBuildingModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForValidationAsync(
            buildingId,
            asTracking: false,
            cancellationToken);

        if (building is null)
            return Result<BuildingAutocorrectionPreview>.NotFound($"Building with id {buildingId} not found.");

        var plan = _planner.CreatePlan(building, request);
        var validation = await _validation.ValidateAsync(buildingId, weatherYear, cancellationToken);
        if (validation.IsFailure)
            return Result<BuildingAutocorrectionPreview>.Failure(validation);

        var response = new BuildingAutocorrectionPreview
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            ProposedActionsCount = plan.Count,
            Actions = plan.Select(item => item.ToContract(applied: false)).ToList(),
            ValidationReport = validation.Value
        };

        return Result<BuildingAutocorrectionPreview>.Success(response);
    }
    
    public async Task<Result<BuildingAutocorrectionResult>> ApplyAsync(
        int buildingId,
        int weatherYear,
        AutocorrectBuildingModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForValidationAsync(
            buildingId,
            asTracking: true,
            cancellationToken);

        if (building is null)
            return Result<BuildingAutocorrectionResult>.NotFound($"Building with id {buildingId} not found.");

        var plan = _planner.CreatePlan(building, request);
        var appliedActions = new List<BuildingAutocorrectionAction>();

        foreach (var item in plan)
        {
            var applyResult = item.Apply();
            if (applyResult.IsSuccess)
                appliedActions.Add(item.ToContract(applied: true));
        }

        if (appliedActions.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        var validation = await _validation.ValidateAsync(buildingId, weatherYear, cancellationToken);
        if (validation.IsFailure)
            return Result<BuildingAutocorrectionResult>.Failure(validation);

        var response = new BuildingAutocorrectionResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            AppliedActionsCount = appliedActions.Count,
            AppliedActions = appliedActions,
            ValidationReport = validation.Value
        };

        return Result<BuildingAutocorrectionResult>.Success(response);
    }
}