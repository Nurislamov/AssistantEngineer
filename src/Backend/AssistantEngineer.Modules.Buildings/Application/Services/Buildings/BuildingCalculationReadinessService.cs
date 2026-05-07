using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class BuildingCalculationReadinessService
{
    private readonly BuildingModelValidationService _validation;

    public BuildingCalculationReadinessService(BuildingModelValidationService validation)
    {
        _validation = validation;
    }

    public async Task<Result<BuildingCalculationReadinessReport>> CheckAsync(
        int buildingId,
        int weatherYear = 2020,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validation.ValidateAsync(
            buildingId,
            weatherYear,
            cancellationToken);

        if (validationResult.IsFailure)
            return Result<BuildingCalculationReadinessReport>.Failure(validationResult);

        var validation = validationResult.Value;

        var report = new BuildingCalculationReadinessReport
        {
            BuildingId = validation.BuildingId,
            BuildingName = validation.BuildingName,
            IsReady = validation.IsValid,
            Issues = validation.Issues
                    .Select(issue => new BuildingCalculationReadinessIssue(
                        issue.Severity,
                        issue.Location,
                        issue.Message))
                    .ToList()
        };

        return Result<BuildingCalculationReadinessReport>.Success(report);
    }
}