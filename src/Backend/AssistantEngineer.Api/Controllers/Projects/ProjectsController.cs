using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Querying.Projects;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Controllers.Projects;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
public class ProjectsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<ProjectResponse>, bool, IOrderedEnumerable<ProjectResponse>>> SortRules =
        new Dictionary<string, Func<IEnumerable<ProjectResponse>, bool, IOrderedEnumerable<ProjectResponse>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, project => project.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, project => project.Name)
                    .ThenByStable(descending, project => project.Id)
        };

    private readonly IBuildingsFacade _buildings;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;
    private readonly IProjectTenantScopedReadService _projectTenantScopedReads;
    private readonly ITenantQueryContextFactory _tenantQueryContextFactory;
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _authorizationOptions;

    public ProjectsController(
        IBuildingsFacade buildings,
        IProtectedEndpointAuthorizationGate authorizationGate,
        IProjectTenantScopedReadService projectTenantScopedReads,
        ITenantQueryContextFactory tenantQueryContextFactory,
        IOptionsMonitor<ApiAuthorizationOptions> authorizationOptions)
    {
        _buildings = buildings;
        _authorizationGate = authorizationGate;
        _projectTenantScopedReads = projectTenantScopedReads;
        _tenantQueryContextFactory = tenantQueryContextFactory;
        _authorizationOptions = authorizationOptions;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequirePermissionAsync(
            Permission.ProjectsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.CreateProjectAsync(
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            project => project.Id);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> GetAll(
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequirePermissionAsync(
            Permission.ProjectsRead,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedProjectReads())
        {
            var tenantContext = _tenantQueryContextFactory.CreateCurrent();
            var scopedResult = await _projectTenantScopedReads.ListProjectsForTenantAsync(
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                return ToTenantScopedReadFailureResult(scopedResult, tenantContext);
            }

            var mapped = scopedResult.Value
                .Select(MapProject)
                .ToList();

            return Result<List<ProjectResponse>>.Success(mapped).ToPagedOkResult(
                this,
                query,
                items => items.ApplyProjectListQuery(query));
        }

        var result = await _buildings.GetProjectsAsync(
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyProjectListQuery(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            id,
            Permission.ProjectsRead,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedProjectReads())
        {
            var tenantContext = _tenantQueryContextFactory.CreateCurrent(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _projectTenantScopedReads.GetProjectForTenantAsync(
                id,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                return ToTenantScopedReadFailureResult(scopedResult, tenantContext);
            }

            var project = scopedResult.Value;
            if (project is null)
            {
                return NotFound();
            }

            return Ok(MapProject(project));
        }

        var result = await _buildings.GetProjectByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> Update(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            id,
            Permission.ProjectsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.UpdateProjectAsync(
            id,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireProjectPermissionAsync(
            id,
            Permission.ProjectsWrite,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToActionResult(authorizationDecision);
        }

        var result = await _buildings.DeleteProjectAsync(
            id,
            cancellationToken);

        return result.ToNoContentResult(this);
    }

    private ActionResult ToActionResult(ProtectedEndpointAuthorizationDecision decision)
    {
        return decision.Outcome switch
        {
            ProtectedEndpointAuthorizationOutcome.Unauthorized => Unauthorized(),
            ProtectedEndpointAuthorizationOutcome.Forbidden => Forbid(),
            ProtectedEndpointAuthorizationOutcome.NotFound => NotFound(),
            _ => Ok()
        };
    }

    private bool ShouldUseTenantScopedProjectReads()
    {
        var options = _authorizationOptions.CurrentValue;
        return options.RequiresProtectedReadAuthorization(Permission.ProjectsRead);
    }

    private ActionResult ToTenantScopedReadFailureResult(
        Result result,
        TenantQueryContext context)
    {
        if (result.ErrorType == ResultErrorType.NotFound)
        {
            return NotFound();
        }

        return result.Error switch
        {
            TenantQueryFailureReasons.Unauthenticated => Unauthorized(),
            TenantQueryFailureReasons.MissingPermission => Forbid(),
            TenantQueryFailureReasons.TenantMismatch or
                TenantQueryFailureReasons.MissingOrganization or
                TenantQueryFailureReasons.UnscopedResourceDenied =>
                context.ReturnNotFoundForTenantMismatch ? NotFound() : Forbid(),
            _ => Forbid()
        };
    }

    private static ProjectResponse MapProject(Project project) =>
        new()
        {
            Id = project.Id,
            Name = project.Name
        };
}
