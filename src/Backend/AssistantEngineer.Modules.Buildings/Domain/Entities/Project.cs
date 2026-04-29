using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.Entities;

public class Project
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CalculationPreferences? Preferences { get; private set; }

    private readonly List<Building> _buildings = new();
    public IReadOnlyCollection<Building> Buildings => new ReadOnlyCollection<Building>(_buildings);

    private Project() { }

    private Project(string name)
    {
        Name = name;
    }

    public static Result<Project> Create(string name)
    {
        var nameResult = name.ToRequiredTrimmed("Project name", maxLength: 200, minLength: 2);
        if (nameResult.IsFailure) return Result<Project>.Failure(nameResult);

        return Result<Project>.Success(new Project(nameResult.Value));
    }

    public Result UpdateName(string name)
    {
        var nameResult = name.ToRequiredTrimmed("Project name", maxLength: 200, minLength: 2);
        if (nameResult.IsFailure) return nameResult;

        Name = nameResult.Value;
        return Result.Success();
    }

    public Result AddBuilding(Building building)
    {
        if (_buildings.Any(b => b.Name.Equals(building.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.Conflict($"Building with name '{building.Name}' already exists in this project.");

        _buildings.Add(building);
        return Result.Success();
    }

    public Result RemoveBuilding(int buildingId)
    {
        var building = _buildings.FirstOrDefault(b => b.Id == buildingId);
        if (building == null)
            return Result.NotFound($"Building with id {buildingId} not found.");

        _buildings.Remove(building);
        return Result.Success();
    }

    public Result SetPreferences(CalculationPreferences preferences)
    {
        if (Preferences != null)
            return Result.Conflict("Project already has calculation preferences.");

        Preferences = preferences;
        return Result.Success();
    }
}
