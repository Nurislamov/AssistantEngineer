using System.Reflection;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers.Analysis;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.Api;

public class BuildingEnergyAnalysisControllerIso52016SimulationEndpointTests
{
    [Fact]
    public void SimulateIso52016_ExposesPostEndpointWithLongRunningTimeout()
    {
        var method = typeof(BuildingEnergyAnalysisController).GetMethod(
            nameof(BuildingEnergyAnalysisController.SimulateIso52016));

        Assert.NotNull(method);

        var post = Assert.Single(
            method.GetCustomAttributes<HttpPostAttribute>());

        Assert.Equal("iso52016/simulate", post.Template);

        var timeout = Assert.Single(
            method.GetCustomAttributes<RequestTimeoutAttribute>());

        Assert.Equal(RequestPolicies.LongRunning, timeout.PolicyName);

        Assert.Equal(
            typeof(Task<ActionResult<Iso52016BuildingEnergySimulationApplicationResult>>),
            method.ReturnType);
    }
}