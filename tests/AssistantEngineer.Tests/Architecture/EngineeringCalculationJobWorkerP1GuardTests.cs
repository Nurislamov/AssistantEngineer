using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.Hosting;

namespace AssistantEngineer.Tests.Architecture;

public class EngineeringCalculationJobWorkerP1GuardTests
{
    [Fact]
    public void JobServiceNoLongerEmitsWorkerNotEnabledDiagnosticForQueuedJobs()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "EngineeringCalculationJobService.cs"));

        Assert.DoesNotContain("CALCULATION_JOB_WORKER_NOT_ENABLED", source, StringComparison.Ordinal);
        Assert.Contains("ExecuteQueuedJobAsync", source, StringComparison.Ordinal);
        Assert.Contains("ExecuteClaimedJobAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void JobRepositoryExposesBoundedQueuedJobReadPath()
    {
        Assert.Contains(
            nameof(IEngineeringCalculationJobRepository.ListQueuedAsync),
            typeof(IEngineeringCalculationJobRepository)
                .GetMethods()
                .Select(method => method.Name));
        Assert.Contains(
            nameof(IEngineeringCalculationJobRepository.TryClaimQueuedJobAsync),
            typeof(IEngineeringCalculationJobRepository)
                .GetMethods()
                .Select(method => method.Name));
    }

    [Fact]
    public void BackgroundWorkerIsAHostedServiceAndUsesScopedRepositories()
    {
        Assert.True(typeof(BackgroundService).IsAssignableFrom(typeof(EngineeringCalculationJobWorker)));

        var source = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "Services",
            "Calculations",
            "Jobs",
            "EngineeringCalculationJobWorker.cs"));

        Assert.Contains("IServiceScopeFactory", source, StringComparison.Ordinal);
        Assert.Contains("ListQueuedAsync", source, StringComparison.Ordinal);
        Assert.Contains("TryClaimQueuedJobAsync", source, StringComparison.Ordinal);
        Assert.Contains("ExecuteClaimedJobAsync", source, StringComparison.Ordinal);
    }
}
