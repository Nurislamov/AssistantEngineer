using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Construction;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
    public DbSet<HourlyClimateData> HourlyClimateData { get; set; } = null!;
    public DbSet<ThermalZone> ThermalZones { get; set; } = null!;
    public DbSet<ThermalZoneRoom> ThermalZoneRooms { get; set; } = null!;
    public DbSet<AnnualClimateData> AnnualClimateData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
