using Microsoft.EntityFrameworkCore;
using AssistantEngineer.Data;
using AssistantEngineer.Services.Calculations;
using AssistantEngineer.Services.Reports;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<RoomCalculationService>();
builder.Services.AddScoped<AggregateCalculationService>();
builder.Services.AddScoped<BuildingReportDataService>();
builder.Services.AddScoped<ExcelReportService>();
builder.Services.AddScoped<EquipmentSelectionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
