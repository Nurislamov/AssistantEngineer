using Microsoft.EntityFrameworkCore;
using AssistantEngineer.Data;
using AssistantEngineer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// регистрация сервиса расчёта
builder.Services.AddScoped<RoomCalculationService>();
builder.Services.AddScoped<StructureCalculationService>();
builder.Services.AddScoped<BuildingReportService>();
builder.Services.AddScoped<ExcelReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
