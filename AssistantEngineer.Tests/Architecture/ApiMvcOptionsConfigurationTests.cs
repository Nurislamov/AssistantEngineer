using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Architecture;

public class ApiMvcOptionsConfigurationTests
{
    [Fact]
    public void ApiMvcOptionsSetupImplementsMvcOptionsConfiguration()
    {
        Assert.True(
            typeof(IConfigureOptions<MvcOptions>).IsAssignableFrom(typeof(ApiMvcOptionsSetup)));
    }

    [Fact]
    public void ApiMvcOptionsSetupRegistersGlobalFilters()
    {
        var options = new MvcOptions();
        var setup = new ApiMvcOptionsSetup();

        setup.Configure(options);

        var serviceFilterTypes = options.Filters
            .OfType<ServiceFilterAttribute>()
            .Select(filter => filter.ServiceType)
            .ToArray();

        Assert.Contains(
            typeof(ValidationFilter),
            serviceFilterTypes);

        Assert.Contains(
            typeof(GlobalExceptionFilter),
            serviceFilterTypes);
    }
}
