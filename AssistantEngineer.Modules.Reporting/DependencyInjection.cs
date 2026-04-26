using AssistantEngineer.Modules.Reporting.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services)
    {
        services.AddReportingCalculationServices();
        services.AddReportingGenerators();
        services.AddReportingDataServices();
        services.AddReportingFacades();

        return services;
    }
}