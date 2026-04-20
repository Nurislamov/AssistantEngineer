using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.Climate;
using AssistantEngineer.Domain.Models.Construction;
using AssistantEngineer.Domain.Models.Schedules;
using AssistantEngineer.Domain.Models.ThermalZones;
using AssistantEngineer.Domain.Models.Ventilation;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
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
