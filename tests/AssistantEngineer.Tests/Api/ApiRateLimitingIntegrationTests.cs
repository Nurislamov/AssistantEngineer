using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.Api;

public class ApiRateLimitingIntegrationTests
{
    [Fact]
    public async Task HeavyWorkflowEndpoint_UsesRateLimitingPolicy_AndReturns429WhenLimitExceeded()
    {
        await using var factory = new RateLimitingFactory();
        var client = factory.CreateClient();

        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var request = new EngineeringWorkflowReportExportRequestDto(
            new EngineeringWorkflowReportRequestDto(state, RequestedFormat: "Json"));

        var first = await client.PostAsJsonAsync("/api/v1/engineering-workflow/report/export/json", request);
        var second = await client.PostAsJsonAsync("/api/v1/engineering-workflow/report/export/json", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    private sealed class RateLimitingFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("RateLimitingTests");
        }
    }
}
