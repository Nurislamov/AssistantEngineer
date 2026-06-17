using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = null!;

    public DbSet<Building> Buildings { get; set; } = null!;

    public DbSet<Floor> Floors { get; set; } = null!;

    public DbSet<Room> Rooms { get; set; } = null!;

    public DbSet<Window> Windows { get; set; } = null!;

    public DbSet<Wall> Walls { get; set; } = null!;

    public DbSet<CoolingEquipmentCatalogItem> EquipmentCatalogItems { get; set; } = null!;

    public DbSet<ClimateZone> ClimateZones { get; set; } = null!;

    public DbSet<CalculationPreferences> CalculationPreferences { get; set; } = null!;

    public DbSet<Material> Materials { get; set; } = null!;

    public DbSet<ConstructionAssembly> ConstructionAssemblies { get; set; } = null!;

    public DbSet<ConstructionLayer> ConstructionLayers { get; set; } = null!;

    public DbSet<HourlySchedule> HourlySchedules { get; set; } = null!;

    public DbSet<VentilationParameters> VentilationParameters { get; set; } = null!;

    public DbSet<ClimateData> ClimateData { get; set; } = null!;

    public DbSet<DesignDayHourlyData> DesignDayHourlyData { get; set; } = null!;

    public DbSet<AnnualClimateData> AnnualClimateData { get; set; } = null!;

    public DbSet<AnnualHourlyData> AnnualHourlyData { get; set; } = null!;

    public DbSet<ThermalZone> ThermalZones { get; set; } = null!;

    public DbSet<TelegramUserEntity> TelegramUsers { get; set; } = null!;

    public DbSet<TelegramConversationSessionEntity> TelegramConversationSessions { get; set; } = null!;

    public DbSet<TelegramDiagnosticCaseEntity> TelegramDiagnosticCases { get; set; } = null!;

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
