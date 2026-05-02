using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Infrastructure.Seeding;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Api.Controllers.ReferenceData;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/development/demo-data")]
public sealed class DevelopmentDemoDataController : ControllerBase
{
    private readonly IDevelopmentDemoDataSeeder _seeder;

    public DevelopmentDemoDataController(IDevelopmentDemoDataSeeder seeder)
    {
        _seeder = seeder;
    }

    [HttpPost("seed")]
    public async Task<ActionResult<DevelopmentDemoSeedResult>> Seed(CancellationToken cancellationToken)
    {
        var environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (environment is null || !environment.IsDevelopment())
            return NotFound();

        var result = await _seeder.SeedAsync(cancellationToken);
        return result.ToActionResult(this);
    }
}
