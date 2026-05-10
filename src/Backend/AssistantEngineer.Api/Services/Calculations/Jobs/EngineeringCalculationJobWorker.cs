using AssistantEngineer.Api.Services.Calculations.Persistence;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<EngineeringCalculationJobWorkerOptions> _options;
    private readonly ILogger<EngineeringCalculationJobWorker> _logger;

    public EngineeringCalculationJobWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EngineeringCalculationJobWorkerOptions> options,
        ILogger<EngineeringCalculationJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Engineering calculation job worker is disabled by configuration.");
            return;
        }

        _logger.LogInformation("Engineering calculation job worker started.");
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

        var jobs = await jobRepository.ListQueuedAsync(Math.Max(1, _options.Value.BatchSize), cancellationToken);
        var processed = 0;

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await jobService.ExecuteQueuedJobAsync(job.JobId, cancellationToken);
                if (result is not null)
                {
                    processed++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Queued engineering calculation job {JobId} failed inside worker loop.", job.JobId);
            }
        }

        return processed;
    }
}