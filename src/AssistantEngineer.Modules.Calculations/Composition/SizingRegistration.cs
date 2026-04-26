using AssistantEngineer.Modules.Calculations.Application.Services.Sizing;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class SizingRegistration
{
    public static IServiceCollection AddSizingCalculations(
        this IServiceCollection services)
    {
        services.AddScoped<BuildingPeakSizingService>();
        services.AddScoped<BuildingReferenceDesignDayService>();
        services.AddScoped<BuildingSyntheticDesignDayService>();

        services.AddScoped<BuildingAutosizingService>();

        services.AddScoped<CatalogAutosizingRankingService>();
        services.AddScoped<BuildingCatalogAutosizingService>();

        services.AddScoped<EquipmentRecommendationService>();
        services.AddScoped<EquipmentRecommendationComparisonService>();

        services.AddScoped<EquipmentRecommendationReportService>();
        services.AddScoped<EquipmentRecommendationComparisonReportService>();

        return services;
    }
}