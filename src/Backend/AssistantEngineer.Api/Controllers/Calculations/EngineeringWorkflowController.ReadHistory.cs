using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Querying.Projects;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

public sealed partial class EngineeringWorkflowController
{
    [HttpGet("{projectId:int}/state")]
    public async Task<ActionResult<EngineeringWorkflowStateDto>> GetWorkflowState(
        int projectId,
        [FromQuery] int? buildingId,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: null,
            jobId: null,
            projectId: projectId,
            buildingId: buildingId,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedWorkflowReads())
        {
            var tenantContext = CreateWorkflowTenantContext(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _workflowTenantScopedReads.GetWorkflowStateForTenantAsync(
                projectId,
                buildingId,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                return ToTenantScopedWorkflowReadFailureResult(scopedResult, tenantContext);
            }

            var persisted = scopedResult.Value.PersistedState;
            if (persisted is not null)
            {
                return Ok(persisted);
            }
        }

        var persistedState = await _workflowPersistence.GetLatestWorkflowStateAsync(
            projectId,
            buildingId,
            cancellationToken);

        if (persistedState is not null)
        {
            return Ok(persistedState);
        }

        EngineeringWorkflowStateDto state;

        try
        {
            state = await _stateBuilder.BuildWorkflowStateAsync(projectId, buildingId, cancellationToken);
            state = _workflowDiagnostics.AddMissingPersistedStateDiagnostic(state, _workflowPersistence.GetProviderInfo());
            await _workflowPersistence.SaveWorkflowStateAsync(state, state.Diagnostics, cancellationToken);
        }
        catch (Exception exception)
        {
            state = _stateBuilder.BuildInfrastructureFallbackState(
                projectId,
                buildingId,
                $"Workflow persistence source is unavailable: {exception.Message}");
        }

        return Ok(state);
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<EngineeringCalculationJobResultDto>> GetJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: null,
            jobId: jobId,
            projectId: null,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedWorkflowReads())
        {
            var tenantContext = CreateWorkflowTenantContext(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _workflowTenantScopedReads.GetJobForTenantAsync(
                jobId,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                if (scopedResult.ErrorType == ResultErrorType.NotFound)
                {
                    return NotFound(new
                    {
                        jobId,
                        code = "CALCULATION_JOB_NOT_FOUND",
                        message = "Calculation job was not found in workflow persistence store."
                    });
                }

                return ToTenantScopedWorkflowReadFailureResult(scopedResult, tenantContext);
            }

            return Ok(scopedResult.Value);
        }

        var result = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (result is null)
        {
            return NotFound(new
            {
                jobId,
                code = "CALCULATION_JOB_NOT_FOUND",
                message = "Calculation job was not found in workflow persistence store."
            });
        }

        authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: result.ScenarioId,
            jobId: jobId,
            projectId: result.ProjectId,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        return Ok(result);
    }

    [HttpGet("jobs/{jobId}/events")]
    public async Task<ActionResult<IReadOnlyList<EngineeringCalculationJobEventDto>>> GetJobEvents(
        string jobId,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: null,
            jobId: jobId,
            projectId: null,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedWorkflowReads())
        {
            var tenantContext = CreateWorkflowTenantContext(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _workflowTenantScopedReads.GetJobEventsForTenantAsync(
                jobId,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                return ToTenantScopedWorkflowReadFailureResult(scopedResult, tenantContext);
            }

            return Ok(scopedResult.Value);
        }

        var job = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (job is not null)
        {
            authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
                workflowId: null,
                scenarioId: job.ScenarioId,
                jobId: jobId,
                projectId: job.ProjectId,
                buildingId: null,
                cancellationToken);
            if (!authorizationDecision.IsAllowed)
            {
                return ToAuthorizationActionResult(authorizationDecision);
            }
        }

        var events = await _jobService.ListJobEventsAsync(jobId, cancellationToken);
        return Ok(events);
    }

    [HttpGet("{projectId:int}/jobs")]
    public async Task<ActionResult<PagedResponse<EngineeringCalculationJobResultDto>>> ListProjectJobs(
        int projectId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: null,
            jobId: null,
            projectId: projectId,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedWorkflowReads())
        {
            var tenantContext = CreateWorkflowTenantContext(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _workflowTenantScopedReads.ListJobsForProjectForTenantAsync(
                projectId,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                return ToTenantScopedWorkflowReadFailureResult(scopedResult, tenantContext);
            }

            return Ok(scopedResult.Value
                .ApplyProjectListQuery(query)
                .ToPagedResponse(NormalizeWorkflowListQuery(query)));
        }

        var jobs = await _jobService.ListProjectJobsAsync(projectId, cancellationToken);
        return Ok(jobs
            .ApplyProjectListQuery(query)
            .ToPagedResponse(NormalizeWorkflowListQuery(query)));
    }

    [HttpGet("scenarios/{scenarioId}")]
    public async Task<ActionResult<EngineeringCalculationScenarioRecordDto>> GetScenarioResult(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: scenarioId,
            jobId: null,
            projectId: null,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedWorkflowReads())
        {
            var tenantContext = CreateWorkflowTenantContext(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _workflowTenantScopedReads.GetScenarioForTenantAsync(
                scenarioId,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                if (scopedResult.ErrorType == ResultErrorType.NotFound)
                {
                    return NotFound(new
                    {
                        scenarioId,
                        code = "WORKFLOW_SCENARIO_NOT_FOUND",
                        message = "Scenario record was not found in workflow persistence foundation store."
                    });
                }

                return ToTenantScopedWorkflowReadFailureResult(scopedResult, tenantContext);
            }

            return Ok(scopedResult.Value);
        }

        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        if (scenario is null)
        {
            return NotFound(new
            {
                scenarioId,
                code = "WORKFLOW_SCENARIO_NOT_FOUND",
                message = "Scenario record was not found in workflow persistence foundation store."
            });
        }

        authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: scenarioId,
            jobId: null,
            projectId: scenario.ProjectId,
            buildingId: scenario.BuildingId,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        return Ok(scenario);
    }

    [HttpGet("{projectId:int}/scenarios")]
    public async Task<ActionResult<PagedResponse<EngineeringCalculationScenarioRecordDto>>> GetProjectScenarios(
        int projectId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: null,
            jobId: null,
            projectId: projectId,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (ShouldUseTenantScopedWorkflowReads())
        {
            var tenantContext = CreateWorkflowTenantContext(includeUnscopedResourcesInTenantLists: false);
            var scopedResult = await _workflowTenantScopedReads.ListScenariosForProjectForTenantAsync(
                projectId,
                tenantContext,
                cancellationToken);
            if (scopedResult.IsFailure)
            {
                return ToTenantScopedWorkflowReadFailureResult(scopedResult, tenantContext);
            }

            return Ok(scopedResult.Value
                .ApplyProjectListQuery(query)
                .ToPagedResponse(NormalizeWorkflowListQuery(query)));
        }

        var scenarios = await _workflowPersistence.ListProjectScenariosAsync(projectId, cancellationToken);
        return Ok(scenarios
            .ApplyProjectListQuery(query)
            .ToPagedResponse(NormalizeWorkflowListQuery(query)));
    }

    private ActionResult ToTenantScopedWorkflowReadFailureResult(
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
}
