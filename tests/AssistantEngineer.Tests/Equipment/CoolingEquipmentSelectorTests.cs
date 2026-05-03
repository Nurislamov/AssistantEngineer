using System.Reflection;
using AssistantEngineer.Modules.Equipment.Application.Services;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public sealed class CoolingEquipmentSelectorTests
{
    [Fact]
    public void SelectForRoomReturnsSuccessWithCompletedSelectionWhenCandidateFound()
    {
        var selector = new CoolingEquipmentSelector();
        var catalog = new[]
        {
            CreateCatalogItem(1, "Acme", "Split", "Wall", "Small", capacityKw: 4.0),
            CreateCatalogItem(2, "Acme", "Split", "Wall", "Right", capacityKw: 6.0),
            CreateCatalogItem(3, "Acme", "Split", "Wall", "Large", capacityKw: 8.0),
            CreateCatalogItem(4, "Acme", "VRF", "Wall", "Wrong system", capacityKw: 10.0)
        };

        var result = selector.SelectForRoom(
            roomId: 101,
            systemType: "Split",
            unitType: "Wall",
            catalog,
            totalHeatLoadKw: 5.0,
            designCapacityKw: 5.5);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value.EquipmentSelected);
        Assert.Equal(2, result.Value.SelectedCatalogItemId);
        Assert.Equal(5.0, result.Value.CoolingLoadKw);
        Assert.Equal(5000, result.Value.RequiredCoolingCapacityW);
        Assert.Equal(5500, result.Value.CapacityWithReserveW);
        Assert.Equal(1.1, result.Value.CoolingSafetyFactor);
        Assert.NotEmpty(result.Value.AcceptedCandidates);
        Assert.Contains(result.Value.RejectedCandidates, candidate =>
            candidate.CatalogItemId == 1 &&
            candidate.Reasons.Contains("Nominal cooling capacity is below required design capacity."));
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "EquipmentSelection.MatrixCoolingSelectorAdapter");
    }

    [Fact]
    public void SelectForRoomReturnsNotFoundWhenNoCandidateMatchesDesignCapacity()
    {
        var selector = new CoolingEquipmentSelector();
        var catalog = new[]
        {
            CreateCatalogItem(1, "Acme", "Split", "Wall", "Small", capacityKw: 4.0)
        };

        var result = selector.SelectForRoom(
            roomId: 101,
            systemType: "Split",
            unitType: "Wall",
            catalog,
            totalHeatLoadKw: 5.0,
            designCapacityKw: 5.5);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public void SelectForRoomReturnsNotFoundWhenCatalogIsEmpty()
    {
        var selector = new CoolingEquipmentSelector();

        var result = selector.SelectForRoom(
            roomId: 101,
            systemType: "Split",
            unitType: "Wall",
            catalog: [],
            totalHeatLoadKw: 5.0,
            designCapacityKw: 5.5);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        Assert.Contains("catalog contains no items", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static CoolingEquipmentCatalogItem CreateCatalogItem(
        int id,
        string manufacturer,
        string systemType,
        string unitType,
        string modelName,
        double capacityKw)
    {
        var item = CoolingEquipmentCatalogItem.Create(
            manufacturer,
            systemType,
            unitType,
            modelName,
            Power.FromWatts(capacityKw * 1000.0).Value).Value;
        SetEntityId(item, id);
        return item;
    }

    private static void SetEntityId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }
}
