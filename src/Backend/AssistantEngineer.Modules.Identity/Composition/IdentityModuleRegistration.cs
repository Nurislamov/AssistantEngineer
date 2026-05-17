using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Application.Services;
using AssistantEngineer.Modules.Identity.Application.Abstractions;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Audit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Identity.Composition;

internal static class IdentityModuleRegistration
{
    public static IServiceCollection AddIdentityModuleServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection("Identity:ProjectTenantAccess");
        services
            .AddOptions<ProjectTenantAccessOptions>()
            .Configure(options =>
            {
                if (bool.TryParse(optionsSection["AllowUnscopedProjectsDuringTransition"], out var allowUnscoped))
                {
                    options.AllowUnscopedProjectsDuringTransition = allowUnscoped;
                }

                if (bool.TryParse(optionsSection["TreatMissingTenantAsBlocking"], out var missingTenantBlocking))
                {
                    options.TreatMissingTenantAsBlocking = missingTenantBlocking;
                }

                if (bool.TryParse(optionsSection["EnableStrictTenantMatch"], out var strictTenantMatch))
                {
                    options.EnableStrictTenantMatch = strictTenantMatch;
                }
            });

        var auditSection = configuration.GetSection(AuditLogOptions.SectionName);
        services
            .AddOptions<AuditLogOptions>()
            .Configure(options =>
            {
                if (bool.TryParse(auditSection["Enabled"], out var enabled))
                {
                    options.Enabled = enabled;
                }

                if (bool.TryParse(auditSection["WriteAuthorizationDeniedEvents"], out var writeAuthorizationDenied))
                {
                    options.WriteAuthorizationDeniedEvents = writeAuthorizationDenied;
                }

                if (bool.TryParse(auditSection["WriteArtifactEvents"], out var writeArtifactEvents))
                {
                    options.WriteArtifactEvents = writeArtifactEvents;
                }

                if (int.TryParse(auditSection["MaxMetadataValueLength"], out var maxMetadataLength))
                {
                    options.MaxMetadataValueLength = maxMetadataLength;
                }

                if (!string.IsNullOrWhiteSpace(auditSection["Provider"]))
                {
                    options.Provider = auditSection["Provider"]!;
                }
            })
            .PostConfigure(options =>
            {
                if (options.MaxMetadataValueLength <= 0)
                {
                    options.MaxMetadataValueLength = 512;
                }

                if (string.IsNullOrWhiteSpace(options.Provider))
                {
                    options.Provider = "InMemory";
                }
            });

        services.AddSingleton<OrganizationPermissionPolicy>();
        services.AddSingleton<ProjectTenantAccessPolicy>();
        services.AddSingleton<ITenantQueryIsolationPolicy, TenantQueryIsolationPolicy>();
        services.AddSingleton<AuditMetadataSanitizer>();
        services.AddSingleton<AuditEventFactory>();
        services.AddSingleton<IAuditLogWriter, InMemoryAuditLogWriter>();
        return services;
    }
}
