using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Weather;
using AssistantEngineer.Modules.Calculations.Application.Services.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Weather;

public class WeatherRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersAnnualWeatherDataNormalizer()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(IAnnualWeatherDataNormalizer));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AnnualClimateDataNormalizer), descriptor.ImplementationType);
    }
}