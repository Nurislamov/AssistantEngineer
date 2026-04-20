using AssistantEngineer.Domain.Models.ThermalZones;
using AssistantEngineer.Infrastructure;
using AssistantEngineer.Infrastructure.Data;
using AssistantEngineer.Infrastructure.Services.Benchmarks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AppDbContextMapsThermalZoneRoomsWithRoomForeignKey()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=AssistantEngineerModelTest;Username=postgres")
            .Options;
        using var context = new AppDbContext(options);

        var entity = context.Model.FindEntityType(typeof(ThermalZoneRoom));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindPrimaryKey());
        Assert.Contains(entity.GetForeignKeys(), key => key.PrincipalEntityType.ClrType.Name == "Room");
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Any(property => property.Name == nameof(ThermalZoneRoom.RoomId)));
    }

    [Fact]
    public void AddInfrastructureBindsEnergyPlusOptionsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                ["EnergyPlus:UseDocker"] = "false",
                ["EnergyPlus:ExecutablePath"] = "C:\\EnergyPlus\\energyplus.exe",
                ["EnergyPlus:DockerUri"] = "tcp://localhost:2375",
                ["EnergyPlus:DockerImage"] = "custom-energyplus:latest",
                ["EnergyPlus:MaxCapturedLogCharacters"] = "4096"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, "Testing");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EnergyPlusBenchmarkOptions>>().Value;
        Assert.False(options.UseDocker);
        Assert.Equal("C:\\EnergyPlus\\energyplus.exe", options.ExecutablePath);
        Assert.Equal("tcp://localhost:2375", options.DockerUri);
        Assert.Equal("custom-energyplus:latest", options.DockerImage);
        Assert.Equal(4096, options.MaxCapturedLogCharacters);
    }
}
