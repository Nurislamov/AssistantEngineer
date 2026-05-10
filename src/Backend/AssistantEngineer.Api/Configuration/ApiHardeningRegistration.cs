using System.Threading.RateLimiting;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiHardeningRegistration
{
    public const string DefaultCorsPolicyName = "ApiCors";
    public const string DefaultRateLimitingPolicyName = "ApiDefault";
    public const string EngineeringHeavyPolicyName = "EngineeringHeavy";
    public const string LivenessTag = "live";
    public const string ReadinessTag = "ready";

    public static WebApplicationBuilder ConfigureApiHardening(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<ApiHardeningOptions>()
            .Bind(builder.Configuration.GetSection(ApiHardeningOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Cors.PolicyName), "API hardening CORS policy name must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RateLimiting.DefaultPolicyName), "API hardening default rate limiting policy name must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RateLimiting.HeavyPolicyName), "API hardening heavy rate limiting policy name must be configured.")
            .Validate(options => options.RateLimiting.PermitLimit > 0, "API hardening rate limiting permit limit must be positive.")
            .Validate(options => options.RateLimiting.WindowSeconds > 0, "API hardening rate limiting window must be positive.")
            .Validate(options => options.RateLimiting.QueueLimit >= 0, "API hardening rate limiting queue limit must be non-negative.")
            .Validate(options => options.RateLimiting.HeavyPermitLimit > 0, "API hardening heavy rate limiting permit limit must be positive.")
            .Validate(options => options.RateLimiting.HeavyWindowSeconds > 0, "API hardening heavy rate limiting window must be positive.")
            .ValidateOnStart();

        var options = ResolveApiHardeningOptions(builder.Configuration);

        builder.Services.AddCors(cors =>
        {
            cors.AddPolicy(options.Cors.PolicyName, policy =>
            {
                if (!options.Cors.Enabled)
                {
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                if (options.Cors.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(options.Cors.AllowedOrigins);
                }
                else
                {
                    // Explicit deny-by-default for production-safe baseline when origins are not configured.
                    policy.SetIsOriginAllowed(_ => false);
                }

                if (options.Cors.AllowedMethods.Length > 0)
                {
                    policy.WithMethods(options.Cors.AllowedMethods);
                }

                if (options.Cors.AllowedHeaders.Length > 0)
                {
                    policy.WithHeaders(options.Cors.AllowedHeaders);
                }
            });
        });

        builder.Services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            if (!options.RateLimiting.Enabled)
            {
                rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("api-hardening-disabled"));
                rateLimiter.AddPolicy(
                    options.RateLimiting.DefaultPolicyName,
                    _ => RateLimitPartition.GetNoLimiter("api-hardening-default-disabled"));
                rateLimiter.AddPolicy(
                    options.RateLimiting.HeavyPolicyName,
                    _ => RateLimitPartition.GetNoLimiter("api-hardening-heavy-disabled"));
                return;
            }

            rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var partitionKey = ResolveRateLimitPartitionKey(context);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.RateLimiting.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.RateLimiting.WindowSeconds),
                        QueueLimit = options.RateLimiting.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = options.RateLimiting.AutoReplenishment
                    });
            });

            rateLimiter.AddPolicy(options.RateLimiting.DefaultPolicyName, _ =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    "default-policy",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.RateLimiting.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.RateLimiting.WindowSeconds),
                        QueueLimit = options.RateLimiting.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = options.RateLimiting.AutoReplenishment
                    });
            });

            rateLimiter.AddPolicy(options.RateLimiting.HeavyPolicyName, _ =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    "heavy-policy",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.RateLimiting.HeavyPermitLimit,
                        Window = TimeSpan.FromSeconds(options.RateLimiting.HeavyWindowSeconds),
                        QueueLimit = options.RateLimiting.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = options.RateLimiting.AutoReplenishment
                    });
            });
        });

        builder.Services
            .AddHealthChecks()
            .AddCheck("api_liveness", () => HealthCheckResult.Healthy("API process is running."), [LivenessTag, ReadinessTag])
            .AddCheck<WorkflowPersistenceReadinessHealthCheck>("workflow_persistence_readiness", tags: [ReadinessTag])
            .AddCheck<DatabaseRegistrationReadinessHealthCheck>("registered_db_contexts", tags: [ReadinessTag]);

        return builder;
    }

    private static ApiHardeningOptions ResolveApiHardeningOptions(IConfiguration configuration)
    {
        var options = new ApiHardeningOptions();
        configuration.GetSection(ApiHardeningOptions.SectionName).Bind(options);

        options.Cors.AllowedOrigins = options.Cors.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        options.Cors.AllowedMethods = options.Cors.AllowedMethods
            .Where(method => !string.IsNullOrWhiteSpace(method))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        options.Cors.AllowedHeaders = options.Cors.AllowedHeaders
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return options;
    }

    private static string ResolveRateLimitPartitionKey(HttpContext context)
    {
        var identity = context.User.Identity;
        if (identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(identity.Name))
        {
            return $"user:{identity.Name}";
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    }

    private sealed class WorkflowPersistenceReadinessHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<EngineeringWorkflowPersistenceOptions> _persistenceOptions;

        public WorkflowPersistenceReadinessHealthCheck(
            IServiceScopeFactory scopeFactory,
            IOptions<EngineeringWorkflowPersistenceOptions> persistenceOptions)
        {
            _scopeFactory = scopeFactory;
            _persistenceOptions = persistenceOptions;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var provider = _persistenceOptions.Value.Provider;
            if (provider != EngineeringWorkflowPersistenceProvider.SQLite)
            {
                return HealthCheckResult.Healthy("Workflow persistence readiness uses non-durable foundation provider in current environment.");
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EngineeringWorkflowPersistenceDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Workflow durable persistence provider is reachable.")
                : HealthCheckResult.Unhealthy("Workflow durable persistence provider is not reachable.");
        }
    }

    private sealed class DatabaseRegistrationReadinessHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseRegistrationReadinessHealthCheck(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContextServices = scope.ServiceProvider
                .GetServices<DbContext>()
                .Select(contextInstance => contextInstance.GetType().FullName ?? contextInstance.GetType().Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (dbContextServices.Length == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy("No DbContext base registrations were resolved in current runtime composition."));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Resolved DbContext registrations: {string.Join(", ", dbContextServices)}."));
        }
    }
}
