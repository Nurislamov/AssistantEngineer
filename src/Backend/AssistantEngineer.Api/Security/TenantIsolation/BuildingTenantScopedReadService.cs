using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public sealed class BuildingTenantScopedReadService : IBuildingTenantScopedReadService
{
    private readonly IBuildingRepository _buildings;
    private readonly IProjectRepository _projects;
    private readonly ITenantQueryIsolationPolicy _policy;

    public BuildingTenantScopedReadService(
        IBuildingRepository buildings,
        IProjectRepository projects,
        ITenantQueryIsolationPolicy policy)
    {
        _buildings = buildings;
        _projects = projects;
        _policy = policy;
    }

    public async Task<Result<Building?>> GetBuildingForTenantAsync(
        int buildingId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetByIdAsync(
            buildingId,
            cancellationToken: cancellationToken);

        if (building is null)
        {
            return Result<Building?>.NotFound($"Building with id {buildingId} not found.");
        }

        var project = await _projects.GetByIdAsync(
            building.ProjectId,
            cancellationToken: cancellationToken);

        if (project is null)
        {
            return Result<Building?>.NotFound($"Building with id {buildingId} not found.");
        }

        var decision = _policy.CanReadResource(
            context,
            project.OrganizationId,
            Permission.BuildingsRead.ToString());

        if (decision.Allowed)
        {
            return Result<Building?>.Success(building);
        }

        return ToDeniedBuildingResult(buildingId, decision);
    }

    public async Task<Result<IReadOnlyList<Building>>> ListBuildingsForProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(
            projectId,
            cancellationToken: cancellationToken);

        if (project is null)
        {
            return Result<IReadOnlyList<Building>>.NotFound($"Project with id {projectId} not found.");
        }

        var decision = _policy.CanReadResource(
            context,
            project.OrganizationId,
            Permission.BuildingsRead.ToString());

        if (!decision.Allowed)
        {
            if (decision.ShouldReturnNotFound)
            {
                return Result<IReadOnlyList<Building>>.NotFound($"Project with id {projectId} not found.");
            }

            return Result<IReadOnlyList<Building>>.Failure(
                CreateDeniedMessage(decision, "Building tenant-scoped list query denied."));
        }

        var buildings = await _buildings.ListByProjectIdAsync(projectId, cancellationToken);

        return Result<IReadOnlyList<Building>>.Success(buildings);
    }

    private static Result<Building?> ToDeniedBuildingResult(
        int buildingId,
        TenantScopedQueryDecision decision)
    {
        if (decision.ShouldReturnNotFound)
        {
            return Result<Building?>.NotFound($"Building with id {buildingId} not found.");
        }

        return Result<Building?>.Failure(
            CreateDeniedMessage(decision, "Building tenant-scoped read denied."));
    }

    private static string CreateDeniedMessage(
        TenantScopedQueryDecision decision,
        string fallback) =>
        string.IsNullOrWhiteSpace(decision.FailureReason)
            ? fallback
            : decision.FailureReason;
}
