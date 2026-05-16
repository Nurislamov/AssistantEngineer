using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Infrastructure.Seeding;
using AssistantEngineer.Modules.Identity.Domain.Enums;
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
    private readonly IAssistantEngineerAuthorizationService _authorizationService;

    public DevelopmentDemoDataController(
        IDevelopmentDemoDataSeeder seeder,
        IAssistantEngineerAuthorizationService authorizationService)
    {
        _seeder = seeder;
        _authorizationService = authorizationService;
    }

    [HttpPost("seed")]
    public async Task<ActionResult<DevelopmentDemoSeedResult>> Seed(CancellationToken cancellationToken)
    {
        var environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (environment is null || !environment.IsDevelopment())
            return NotFound();

        var decision = _authorizationService.AuthorizePilotPermission(Permission.AdministrationManage.ToString());
        if (decision == AssistantEngineerAuthorizationDecision.Unauthorized)
            return Unauthorized();

        if (decision == AssistantEngineerAuthorizationDecision.Forbidden)
            return Forbid();

        var result = await _seeder.SeedAsync(cancellationToken);
        return result.ToActionResult(this);
    }
}
