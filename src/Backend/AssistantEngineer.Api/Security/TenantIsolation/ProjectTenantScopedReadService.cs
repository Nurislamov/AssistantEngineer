using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public sealed class ProjectTenantScopedReadService : IProjectTenantScopedReadService
{
    private readonly IProjectRepository _projects;
    private readonly ITenantQueryIsolationPolicy _policy;

    public ProjectTenantScopedReadService(
        IProjectRepository projects,
        ITenantQueryIsolationPolicy policy)
    {
        _projects = projects;
        _policy = policy;
    }

    public async Task<Result<Project?>> GetProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(
            projectId,
            cancellationToken: cancellationToken);

        if (project is null)
        {
            return Result<Project?>.NotFound($"Project with id {projectId} not found.");
        }

        var decision = _policy.CanReadResource(
            context,
            project.OrganizationId,
            Permission.ProjectsRead.ToString());

        if (decision.Allowed)
        {
            return Result<Project?>.Success(project);
        }

        return ToDeniedProjectResult(projectId, decision);
    }

    public async Task<Result<IReadOnlyList<Project>>> ListProjectsForTenantAsync(
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var listDecision = _policy.CanReadResource(
            context,
            context.OrganizationId,
            Permission.ProjectsRead.ToString());

        if (!listDecision.Allowed)
        {
            return Result<IReadOnlyList<Project>>.Failure(
                CreateDeniedMessage(listDecision, "Project tenant-scoped list query denied."));
        }

        var projects = await _projects.ListAsync(cancellationToken);
        IEnumerable<Project> filteredProjects;

        if (context.OrganizationId.HasValue)
        {
            var organizationId = context.OrganizationId.Value;
            filteredProjects = projects.Where(project =>
                project.OrganizationId == organizationId ||
                (context.IncludeUnscopedResourcesInTenantLists &&
                 context.AllowUnscopedResourcesDuringTransition &&
                 project.OrganizationId == null));
        }
        else if (context.IncludeUnscopedResourcesInTenantLists &&
                 context.AllowUnscopedResourcesDuringTransition)
        {
            filteredProjects = projects.Where(project => project.OrganizationId == null);
        }
        else
        {
            return Result<IReadOnlyList<Project>>.Failure(TenantQueryFailureReasons.MissingOrganization);
        }

        var orderedProjects = filteredProjects
            .OrderBy(project => project.Id)
            .ToList();

        return Result<IReadOnlyList<Project>>.Success(orderedProjects);
    }

    private static Result<Project?> ToDeniedProjectResult(
        int projectId,
        TenantScopedQueryDecision decision)
    {
        if (decision.ShouldReturnNotFound)
        {
            return Result<Project?>.NotFound($"Project with id {projectId} not found.");
        }

        return Result<Project?>.Failure(
            CreateDeniedMessage(decision, "Project tenant-scoped read denied."));
    }

    private static string CreateDeniedMessage(
        TenantScopedQueryDecision decision,
        string fallback) =>
        string.IsNullOrWhiteSpace(decision.FailureReason)
            ? fallback
            : decision.FailureReason;
}
