using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<EngineeringCalculationJobWorkerOptions> _options;
    private readonly ILogger<EngineeringCalculationJobWorker> _logger;
    private readonly string _workerId;

    public EngineeringCalculationJobWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EngineeringCalculationJobWorkerOptions> options,
        ILogger<EngineeringCalculationJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _workerId = ResolveWorkerId(options.Value.WorkerId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Engineering calculation job worker is disabled by configuration.");
            return;
        }

        _logger.LogInformation("Engineering calculation job worker started. WorkerId={WorkerId}", _workerId);
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.Value.PollIntervalSeconds)), stoppingToken);
        }
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IEngineeringCalculationJobRepository>();
        var jobService = scope.ServiceProvider.GetRequiredService<IEngineeringCalculationJobService>();
        var leaseDuration = TimeSpan.FromSeconds(Math.Max(1, _options.Value.LeaseDurationSeconds));

        var jobs = await jobRepository.ListQueuedAsync(Math.Max(1, _options.Value.BatchSize), cancellationToken);
        var processed = 0;

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var claimed = await jobRepository.TryClaimQueuedJobAsync(
                    job.JobId,
                    _workerId,
                    leaseDuration,
                    cancellationToken);
                if (claimed is null)
                {
                    _logger.LogDebug(
                        "Engineering queued calculation job claim skipped because another worker already claimed it. JobId={JobId}, WorkerId={WorkerId}",
                        job.JobId,
                        _workerId);
                    continue;
                }

                var result = await jobService.ExecuteClaimedJobAsync(job.JobId, _workerId, cancellationToken);
                if (result is not null)
                {
                    processed++;
                    _logger.LogInformation(
                        "Engineering queued calculation job processed by worker. WorkerId={WorkerId}, JobId={JobId}, Status={Status}, ProgressPercent={ProgressPercent}",
                        _workerId,
                        result.JobId,
                        result.Status,
                        result.ProgressPercent);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Queued engineering calculation job {JobId} failed inside worker loop.", job.JobId);
            }
        }

        if (jobs.Count > 0)
        {
            _logger.LogInformation(
                "Engineering calculation job worker batch completed. RequestedBatchSize={RequestedBatchSize}, PulledJobs={PulledJobs}, ProcessedJobs={ProcessedJobs}",
                Math.Max(1, _options.Value.BatchSize),
                jobs.Count,
                processed);
        }

        return processed;
    }

    private static string ResolveWorkerId(string? configuredWorkerId)
    {
        if (!string.IsNullOrWhiteSpace(configuredWorkerId))
        {
            return configuredWorkerId.Trim();
        }

        return $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }
}
