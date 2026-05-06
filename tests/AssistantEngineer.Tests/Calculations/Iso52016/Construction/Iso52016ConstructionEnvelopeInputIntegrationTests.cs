using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Construction;

public sealed class Iso52016ConstructionEnvelopeInputIntegrationTests
{
    private static readonly Iso52016RoomSimulationDefaults Defaults = new();

    [Fact]
    public void DefaultOption_PreservesCompatibilityBehavior()
    {
        var room = CreateRoomWithSingleExternalWallAndWindow(wallAreaM2: 10.0, wallUValue: 0.4, windowAreaM2: 2.0, windowUValue: 1.5);

        var calculator = CreateCalculator(new Iso52016ConstructionOptions
        {
            UseConstructionLayerMassInput = false
        });

        var result = calculator.Calculate(room, Defaults);

        Assert.True(result.IsSuccess);
        Assert.Equal(7.0, result.Value.TransmissionHeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(0.34 * room.CalculateVolume() * 0.5, result.Value.VentilationHeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(
            room.CalculateInternalHeatCapacityKjPerK(Defaults.FloorHeatCapacityKjPerM2K, Defaults.CeilingHeatCapacityKjPerM2K) * 1000.0,
            result.Value.ThermalCapacityJPerK,
            precision: 6);
    }

    [Fact]
    public void OptInEquivalentMasslessAssembly_PreservesWallTransmissionForSimpleWall()
    {
        var room = CreateRoomWithSingleExternalWallAndWindow(wallAreaM2: 12.0, wallUValue: 0.5, windowAreaM2: 0.0, windowUValue: 1.0);

        var calculator = CreateCalculator(new Iso52016ConstructionOptions
        {
            UseConstructionLayerMassInput = true,
            UseCalculatedAssemblyUValue = true,
            UseCalculatedAssemblyHeatCapacity = false
        });

        var result = calculator.Calculate(room, Defaults);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.0 * 0.5, result.Value.TransmissionHeatTransferCoefficientWPerK, precision: 4);
    }

    [Fact]
    public void OptInHeatCapacity_UsesAssemblyEffectiveCapacityContribution()
    {
        var room = CreateRoomWithSingleExternalWallAndWindow(wallAreaM2: 10.0, wallUValue: 0.45, windowAreaM2: 0.0, windowUValue: 1.0);
        var adapter = new MassiveEquivalentAssemblyAdapter();
        var assemblyCalculator = new Iso52016ConstructionAssemblyCalculator(new Iso52016ConstructionReferenceDataProvider());
        var calculator = CreateCalculator(
            new Iso52016ConstructionOptions
            {
                UseConstructionLayerMassInput = true,
                UseCalculatedAssemblyUValue = true,
                UseCalculatedAssemblyHeatCapacity = true
            },
            assemblyCalculator,
            adapter);

        var envelope = calculator.Calculate(room, Defaults);
        Assert.True(envelope.IsSuccess);

        var heatTransferWall = Assert.Single(
            room.Walls,
            wall => wall.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned);
        var assembly = Assert.Single(adapter.BuildWallAssemblies(room, Defaults));
        var assemblyResult = assemblyCalculator.Calculate(assembly);

        var compatibilityCapacity = room.CalculateInternalHeatCapacityKjPerK(
            Defaults.FloorHeatCapacityKjPerM2K,
            Defaults.CeilingHeatCapacityKjPerM2K) * 1000.0;
        var expectedCapacity = compatibilityCapacity + heatTransferWall.Area.SquareMeters * assemblyResult.EffectiveInternalHeatCapacityJPerM2K;

        Assert.Equal(expectedCapacity, envelope.Value.ThermalCapacityJPerK, precision: 3);
    }

    [Fact]
    public void OptInWithoutAssemblies_FallsBackToCompatibilityPath()
    {
        var room = CreateRoomWithSingleExternalWallAndWindow(wallAreaM2: 9.0, wallUValue: 0.42, windowAreaM2: 1.5, windowUValue: 1.3);

        var compatibilityCalculator = CreateCalculator(new Iso52016ConstructionOptions
        {
            UseConstructionLayerMassInput = false
        });
        var fallbackCalculator = CreateCalculator(
            new Iso52016ConstructionOptions
            {
                UseConstructionLayerMassInput = true,
                UseCalculatedAssemblyUValue = true,
                UseCalculatedAssemblyHeatCapacity = true
            },
            new Iso52016ConstructionAssemblyCalculator(new Iso52016ConstructionReferenceDataProvider()),
            new EmptyAssemblyAdapter());

        var compatibility = compatibilityCalculator.Calculate(room, Defaults);
        var fallback = fallbackCalculator.Calculate(room, Defaults);

        Assert.True(compatibility.IsSuccess);
        Assert.True(fallback.IsSuccess);
        Assert.Equal(compatibility.Value.TransmissionHeatTransferCoefficientWPerK, fallback.Value.TransmissionHeatTransferCoefficientWPerK, precision: 6);
        Assert.Equal(compatibility.Value.ThermalCapacityJPerK, fallback.Value.ThermalCapacityJPerK, precision: 6);
    }

    [Fact]
    public void VentilationCalculation_IsUnchangedInDefaultAndOptInModes()
    {
        var room = CreateRoomWithSingleExternalWallAndWindow(wallAreaM2: 8.0, wallUValue: 0.38, windowAreaM2: 1.0, windowUValue: 1.2);

        var compatibility = CreateCalculator(new Iso52016ConstructionOptions
        {
            UseConstructionLayerMassInput = false
        }).Calculate(room, Defaults);

        var optIn = CreateCalculator(new Iso52016ConstructionOptions
        {
            UseConstructionLayerMassInput = true,
            UseCalculatedAssemblyUValue = true,
            UseCalculatedAssemblyHeatCapacity = true
        }).Calculate(room, Defaults);

        Assert.True(compatibility.IsSuccess);
        Assert.True(optIn.IsSuccess);
        Assert.Equal(
            compatibility.Value.VentilationHeatTransferCoefficientWPerK,
            optIn.Value.VentilationHeatTransferCoefficientWPerK,
            precision: 6);
    }

    [Fact]
    public void DependencyInjection_RegistersConstructionComponentsAsSingletonWithSafeLifetimes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddCalculationsModule(configuration);

        AssertServiceLifetime<Iso52016ConstructionReferenceDataProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso52016ConstructionAssemblyCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso52016ConstructionAssemblyApplicationAdapter>(services, ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        _ = provider.GetRequiredService<Iso52016ConstructionReferenceDataProvider>();
        _ = provider.GetRequiredService<Iso52016ConstructionAssemblyCalculator>();
        _ = provider.GetRequiredService<Iso52016ConstructionAssemblyApplicationAdapter>();
    }

    private static Iso52016RoomEnvelopeInputCalculator CreateCalculator(
        Iso52016ConstructionOptions options,
        Iso52016ConstructionAssemblyCalculator? assemblyCalculator = null,
        Iso52016ConstructionAssemblyApplicationAdapter? adapter = null)
    {
        return new Iso52016RoomEnvelopeInputCalculator(
            Options.Create(options),
            assemblyCalculator ?? new Iso52016ConstructionAssemblyCalculator(new Iso52016ConstructionReferenceDataProvider()),
            adapter ?? new Iso52016ConstructionAssemblyApplicationAdapter(Options.Create(options)));
    }

    private static Room CreateRoomWithSingleExternalWallAndWindow(
        double wallAreaM2,
        double wallUValue,
        double windowAreaM2,
        double windowUValue)
    {
        var room = CreateBaseRoom();

        var wallResult = room.AddWall(
            Area.FromSquareMeters(wallAreaM2).Value,
            ThermalTransmittance.FromValue(wallUValue).Value,
            CardinalDirection.South,
            WallBoundaryType.External);
        Assert.True(wallResult.IsSuccess);

        if (windowAreaM2 > 0.0)
        {
            var windowResult = room.AddWindow(
                Area.FromSquareMeters(windowAreaM2).Value,
                ThermalTransmittance.FromValue(windowUValue).Value,
                SolarHeatGainCoefficient.FromValue(0.6).Value,
                CardinalDirection.South);
            Assert.True(windowResult.IsSuccess);
        }

        return room;
    }

    private static Room CreateBaseRoom()
    {
        var project = Project.Create("Construction integration project").Value;
        var building = Building.Create("Building", project).Value;
        var floor = Floor.Create("Floor", building).Value;

        return Room.Create(
            name: "Room 1",
            area: Area.FromSquareMeters(20).Value,
            heightM: 3.0,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: null,
            floor: floor,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService));
        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    private sealed class EmptyAssemblyAdapter : Iso52016ConstructionAssemblyApplicationAdapter
    {
        public override IReadOnlyList<Iso52016ConstructionAssembly> BuildWallAssemblies(Room room, Iso52016RoomSimulationDefaults defaults) =>
            [];
    }

    private sealed class MassiveEquivalentAssemblyAdapter : Iso52016ConstructionAssemblyApplicationAdapter
    {
        public override IReadOnlyList<Iso52016ConstructionAssembly> BuildWallAssemblies(Room room, Iso52016RoomSimulationDefaults defaults)
        {
            var wall = Assert.Single(
                room.Walls,
                candidate => candidate.BoundaryType is WallBoundaryType.External or WallBoundaryType.Ground or WallBoundaryType.AdjacentUnconditioned);
            var resolvedUValue = wall.UValue.Value;

            return
            [
                BuildFallbackAssemblyFromUValue(
                    assemblyId: $"wall-{wall.Id}-test-massive",
                    name: "CompatibilityEquivalentTestAssembly",
                    boundaryKind: Iso52016ConstructionBoundaryKind.ExternalWall,
                    areaM2: wall.Area.SquareMeters,
                    resolvedUValueWPerM2K: resolvedUValue,
                    effectiveInternalHeatCapacityJPerM2K: 80_000.0)
            ];
        }
    }
}
