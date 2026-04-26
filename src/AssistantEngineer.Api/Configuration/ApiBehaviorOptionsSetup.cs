using AssistantEngineer.Api.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Configuration;

internal sealed class ApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
{
    public void Configure(
        ApiBehaviorOptions options)
    {
        options.SuppressModelStateInvalidFilter = true;

        options.InvalidModelStateResponseFactory = context =>
            ApiProblemDetailsFactory.CreateValidationResult(
                context,
                "One or more validation errors occurred.");
    }
}