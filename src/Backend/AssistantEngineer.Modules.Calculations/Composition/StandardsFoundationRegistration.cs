using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class StandardsFoundationRegistration
{
    public static IServiceCollection AddStandardsFoundation(
        this IServiceCollection services)
    {
        services.AddSingleton<IStandardCalculationDisclosureFactory, StandardCalculationDisclosureFactory>();
        services.AddSingleton<IAnnualProfileShapeValidator, AnnualProfileShapeValidator>();

        return services;
    }
}
