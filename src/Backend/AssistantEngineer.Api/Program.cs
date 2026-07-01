using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Services.DatabaseMigrations;

if (PostgresMigrationCommand.IsMigrationCommand(args))
{
    return await PostgresMigrationCommand.RunAsync(args);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddApiConfiguration();

builder.ConfigureRequestLimits();
builder.ConfigureApiHardening();
builder.ConfigureDataProtection();

builder.Services.AddApiPresentation();
builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddApiVersioningSupport();
builder.Services.AddApiDocumentation();
builder.Services.AddAssistantEngineerModules(
    builder.Configuration,
    builder.Environment.EnvironmentName);

var app = builder.Build();

app.UseApiPipeline();

app.Run();
return 0;

public partial class Program;
