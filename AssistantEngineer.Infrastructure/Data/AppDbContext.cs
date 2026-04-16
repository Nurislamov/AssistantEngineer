using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Window> Windows { get; set; }
    public DbSet<Wall> Walls { get; set; }
    public DbSet<CoolingEquipmentCatalogItem> EquipmentCatalogItems { get; set; }

    IQueryable<Project> IAppDbContext.Projects => Projects;
    IQueryable<Building> IAppDbContext.Buildings => Buildings;
    IQueryable<Floor> IAppDbContext.Floors => Floors;
    IQueryable<Room> IAppDbContext.Rooms => Rooms;
    IQueryable<Window> IAppDbContext.Windows => Windows;
    IQueryable<Wall> IAppDbContext.Walls => Walls;
    IQueryable<CoolingEquipmentCatalogItem> IAppDbContext.EquipmentCatalogItems => EquipmentCatalogItems;

    public void AddProject(Project project) => Projects.Add(project);
    public void AddBuilding(Building building) => Buildings.Add(building);
    public void AddFloor(Floor floor) => Floors.Add(floor);
    public void AddRoom(Room room) => Rooms.Add(room);
    public void AddWindow(Window window) => Windows.Add(window);
    public void AddWall(Wall wall) => Walls.Add(wall);
    public void AddCoolingEquipmentCatalogItem(CoolingEquipmentCatalogItem item) => EquipmentCatalogItems.Add(item);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoolingEquipmentCatalogItem>()
            .ToTable("EquipmentCatalogItems");

        base.OnModelCreating(modelBuilder);
    }
}
