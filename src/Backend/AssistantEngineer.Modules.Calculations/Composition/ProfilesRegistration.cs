using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class ProfilesRegistration
{
    public static IServiceCollection AddCalculationProfiles(
        this IServiceCollection services)
    {
        services.AddSingleton<IAnnualScheduleGenerator, AnnualScheduleGenerationService>();
        services.AddSingleton<AnnualProfileGenerationService>();

        services.AddSingleton<IRoomAnnualProfileSetProvider, RoomAnnualProfileSetProvider>();
        services.AddSingleton<IHourlyRoomProfileAccessor, HourlyRoomProfileAccessor>();

        services.AddSingleton<IHourlyProfileAggregator, HourlyProfileAggregator>();
        services.AddSingleton<IAnnualProfileGenerator, AnnualProfileGenerator>();

        services.AddScoped<En16798ProfileService>();

        return services;
    }
}