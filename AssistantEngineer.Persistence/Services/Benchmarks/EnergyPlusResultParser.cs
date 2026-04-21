using System.Text.Json;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Persistence.Services.Benchmarks;

public class EnergyPlusResultParser : IEnergyPlusResultParser
{
    private readonly ILogger<EnergyPlusResultParser> _logger;

    public EnergyPlusResultParser(ILogger<EnergyPlusResultParser> logger)
    {
        _logger = logger;
    }

    public EnergyPlusCalculationSummary Parse(string outputDirectory)
    {
        var jsonPath = Path.Combine(outputDirectory, "eplusout.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("EnergyPlus JSON output not found at {JsonPath}", jsonPath);
            return new EnergyPlusCalculationSummary();
        }

        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);

        var summary = new EnergyPlusCalculationSummary();

        // Ищем почасовые данные
        if (doc.RootElement.TryGetProperty("Outputs", out var outputs) &&
            outputs.TryGetProperty("Output:Variable", out var variables))
        {
            var hourlyCooling = new double[24];
            var hourlyHeating = new double[24];

            foreach (var variable in variables.EnumerateArray())
            {
                var name = variable.GetProperty("VariableName").GetString();
                if (name == "Zone Ideal Loads Supply Air Total Cooling Energy")
                {
                    FillHourlyValues(variable, hourlyCooling);
                }
                else if (name == "Zone Ideal Loads Supply Air Total Heating Energy")
                {
                    FillHourlyValues(variable, hourlyHeating);
                }
            }

            summary.HourlyCoolingLoadW = hourlyCooling.ToList();
            summary.HourlyHeatingLoadW = hourlyHeating.ToList();
            summary.PeakCoolingLoadW = hourlyCooling.Max();
            summary.PeakHeatingLoadW = hourlyHeating.Max();
        }

        return summary;
    }

    private static void FillHourlyValues(JsonElement variable, double[] targetArray)
    {
        if (variable.TryGetProperty("Values", out var values))
        {
            int hour = 0;
            foreach (var value in values.EnumerateArray())
            {
                if (hour >= 24) break;
                targetArray[hour++] += value.GetDouble(); // Суммируем по всем зонам
            }
        }
    }
}