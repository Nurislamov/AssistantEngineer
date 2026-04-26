using AssistantEngineer.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Configuration;

internal sealed class ApiMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    public void Configure(
        MvcOptions options)
    {
        options.Filters.AddService<ValidationFilter>();
        options.Filters.AddService<GlobalExceptionFilter>();
    }
}