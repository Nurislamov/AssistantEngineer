using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Controllers;
using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests;

public class EquipmentCatalogControllerTests
{
    [Fact]
    public async Task Create_WhenRequestIsValid_ReturnsCreatedResultAndPersistsItem()
    {
        // Arrange
        await using var context = CreateContext();
        var controller = new EquipmentCatalogController(new CoolingEquipmentCatalogService(context));
        var request = new CreateEquipmentCatalogItemRequest
        {
            Manufacturer = "ACME",
            SystemType = "Split",
            UnitType = "WallMounted",
            ModelName = "WM-3.5",
            NominalCoolingCapacityKw = 3.5,
            IsActive = true
        };

        // Act
        var actionResult = await controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(nameof(EquipmentCatalogController.GetById), createdResult.ActionName);

        var response = Assert.IsType<EquipmentCatalogItemResponse>(createdResult.Value);
        Assert.True(response.Id > 0);
        Assert.Equal("ACME", response.Manufacturer);
        Assert.Equal("Split", response.SystemType);
        Assert.Equal("WallMounted", response.UnitType);
        Assert.Equal("WM-3.5", response.ModelName);
        Assert.Equal(3.5, response.NominalCoolingCapacityKw);
        Assert.True(response.IsActive);
        Assert.Equal(response.Id, createdResult.RouteValues?["id"]);

        var persisted = await context.EquipmentCatalogItems.SingleAsync();
        Assert.Equal(response.Id, persisted.Id);
        Assert.Equal("ACME", persisted.Manufacturer);
        Assert.Equal("Split", persisted.SystemType);
        Assert.Equal("WallMounted", persisted.UnitType);
        Assert.Equal("WM-3.5", persisted.ModelName);
        Assert.Equal(3.5, persisted.NominalCoolingCapacityKw);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task GetById_WhenItemDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateContext();
        var controller = new EquipmentCatalogController(new CoolingEquipmentCatalogService(context));

        // Act
        var actionResult = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetAll_ReturnsItemsOrderedBySystemTypeUnitTypeAndCapacity()
    {
        // Arrange
        await using var context = CreateContext();
        context.EquipmentCatalogItems.AddRange(
            new CoolingEquipmentCatalogItem
            {
                Id = 1,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "WallMounted",
                ModelName = "WM-3.5",
                NominalCoolingCapacityKw = 3.5,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 2,
                Manufacturer = "ACME",
                SystemType = "VRF",
                UnitType = "Cassette",
                ModelName = "VRF-CAS-7.1",
                NominalCoolingCapacityKw = 7.1,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 3,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "WallMounted",
                ModelName = "WM-2.6",
                NominalCoolingCapacityKw = 2.6,
                IsActive = true
            },
            new CoolingEquipmentCatalogItem
            {
                Id = 4,
                Manufacturer = "ACME",
                SystemType = "Split",
                UnitType = "Cassette",
                ModelName = "CAS-3.0",
                NominalCoolingCapacityKw = 3.0,
                IsActive = true
            });
        await context.SaveChangesAsync();

        var controller = new EquipmentCatalogController(new CoolingEquipmentCatalogService(context));

        // Act
        var actionResult = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<EquipmentCatalogItemResponse>>(okResult.Value)
            .ToList();

        Assert.Equal(4, items.Count);
        Assert.Equal([4, 3, 1, 2], items.Select(item => item.Id).ToArray());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
