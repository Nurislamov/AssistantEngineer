using AssistantEngineer.Infrastructure.Persistence.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Infrastructure.Composition;

internal static class RepositoryRegistration
{
    public static IServiceCollection AddRepositoryAdapters(
        this IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IClimateZoneRepository, ClimateZoneRepository>();
        services.AddScoped<IClimateDataRepository, ClimateDataRepository>();
        services.AddScoped<IBuildingHeatingReadModelRepository, BuildingHeatingReadModelRepository>();
        services.AddScoped<IAnnualClimateDataRepository, AnnualClimateDataRepository>();
        services.AddScoped<ICalculationPreferencesRepository, CalculationPreferencesRepository>();
        services.AddScoped<IEquipmentCatalogRepository, EquipmentCatalogRepository>();
        services.RemoveAll<ITelegramUserStore>();
        services.AddSingleton<ITelegramUserStore, EfTelegramUserStore>();
        services.RemoveAll<ITelegramConversationSessionStore>();
        services.AddSingleton<ITelegramConversationSessionStore, EfTelegramConversationSessionStore>();
        services.RemoveAll<ITelegramDiagnosticCaseStore>();
        services.AddSingleton<ITelegramDiagnosticCaseStore, EfTelegramDiagnosticCaseStore>();
        services.RemoveAll<ITelegramServiceRequestStore>();
        services.AddSingleton<ITelegramServiceRequestStore, EfTelegramServiceRequestStore>();
        services.RemoveAll<ITelegramServiceRequestEventStore>();
        services.AddSingleton<ITelegramServiceRequestEventStore, EfTelegramServiceRequestEventStore>();
        services.RemoveAll<ITelegramServiceRequestDialogStore>();
        services.AddSingleton<ITelegramServiceRequestDialogStore, EfTelegramServiceRequestDialogStore>();
        services.RemoveAll<ITelegramUserAuditEventStore>();
        services.AddSingleton<ITelegramUserAuditEventStore, EfTelegramUserAuditEventStore>();
        services.RemoveAll<ITelegramManualFileBindingStore>();
        services.AddSingleton<ITelegramManualFileBindingStore, EfTelegramManualFileBindingStore>();
        services.RemoveAll<ITelegramLibraryAccessStore>();
        services.AddSingleton<ITelegramLibraryAccessStore, EfTelegramLibraryAccessStore>();
        services.RemoveAll<ITelegramOperatorInboxStore>();
        services.AddSingleton<ITelegramOperatorInboxStore, EfTelegramOperatorInboxStore>();
        services.RemoveAll<ITelegramBroadcastStore>();
        services.AddSingleton<ITelegramBroadcastStore, EfTelegramBroadcastStore>();

        return services;
    }
}
