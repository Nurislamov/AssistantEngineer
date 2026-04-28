using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.WeatherSolar;

public class WeatherSolarRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersAnnualWeatherSolarProfileBuilder()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(IAnnualWeatherSolarProfileBuilder));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AnnualWeatherSolarProfileBuilder), descriptor.ImplementationType);
    }
}