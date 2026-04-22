using AssistantEngineer.Api;
using AssistantEngineer.Api.Facades;
using AssistantEngineer.Api.Filters;
using Asp.Versioning;
using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Equipment;
using AssistantEngineer.Modules.Reporting;
using AssistantEngineer.Infrastructure;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("building-archetypes.json", optional: false, reloadOnChange: true);

var maxRequestBodyBytes = builder.Configuration.GetValue<long?>("RequestLimits:MaxRequestBodyBytes") ?? 1_048_576;
var defaultRequestTimeoutSeconds = builder.Configuration.GetValue<int?>("RequestLimits:DefaultTimeoutSeconds") ?? 30;
var longRunningRequestTimeoutSeconds = builder.Configuration.GetValue<int?>("RequestLimits:LongRunningTimeoutSeconds") ?? 600;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(defaultRequestTimeoutSeconds),
        TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
    };

    options.AddPolicy(RequestPolicies.LongRunning, new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(longRunningRequestTimeoutSeconds),
        TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<GlobalExceptionFilter>();
});
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddBuildingsModule(builder.Configuration);
builder.Services.AddCalculationsModule(builder.Configuration);
builder.Services.AddEquipmentModule();
builder.Services.AddReportingModule();
builder.Services.AddBenchmarksModule(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddScoped<IBuildingsFacade, BuildingsFacade>();
builder.Services.AddScoped<IRoomsFacade, RoomsFacade>();
builder.Services.AddScoped<IReportsFacade, ReportsFacade>();
builder.Services.AddScoped<IBenchmarksFacade, BenchmarksFacade>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRequestTimeouts();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
