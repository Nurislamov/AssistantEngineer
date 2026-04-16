using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Abstractions;

public interface IAppDbContext
{
    IQueryable<Project> Projects { get; }
    IQueryable<Building> Buildings { get; }
    IQueryable<Floor> Floors { get; }
    IQueryable<Room> Rooms { get; }
    IQueryable<Window> Windows { get; }
    IQueryable<Wall> Walls { get; }
    IQueryable<CoolingEquipmentCatalogItem> EquipmentCatalogItems { get; }

    void AddProject(Project project);
    void AddBuilding(Building building);
    void AddFloor(Floor floor);
    void AddRoom(Room room);
    void AddWindow(Window window);
    void AddWall(Wall wall);
    void AddCoolingEquipmentCatalogItem(CoolingEquipmentCatalogItem item);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
