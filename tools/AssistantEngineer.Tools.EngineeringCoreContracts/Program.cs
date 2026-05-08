using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssistantEngineer.Tools.EngineeringCoreContracts;

internal static class Program
{
    private const string ManifestPath = "docs/releases/EngineeringCoreV1Manifest.json";
    private const string DiagnosticsCatalogPath = "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json";

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintHelp();
                return 0;
            }

            var repoRoot = FindRepositoryRoot();
            Directory.SetCurrentDirectory(repoRoot);

            var command = args[0];
            var outputDirectory = ReadOption(args, "--output-directory");

            return command switch
            {
                "generate-api-contract-snapshots" => GenerateApiContractSnapshots(outputDirectory ?? "docs/api/engineering-core-v1"),
                "generate-report-contract-snapshots" => GenerateReportContractSnapshots(outputDirectory ?? "docs/reports/engineering-core-v1"),
                "generate-all-contract-snapshots" => GenerateAllContractSnapshots(),
                _ => Unknown(command)
            };
        }
        catch (Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception.Message);
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer Engineering Core contract snapshot tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  generate-api-contract-snapshots [--output-directory <path>]");
        Console.WriteLine("  generate-report-contract-snapshots [--output-directory <path>]");
        Console.WriteLine("  generate-all-contract-snapshots");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int GenerateAllContractSnapshots()
    {
        var apiCode = GenerateApiContractSnapshots("docs/api/engineering-core-v1");
        if (apiCode != 0)
            return apiCode;

        return GenerateReportContractSnapshots("docs/reports/engineering-core-v1");
    }

    private static int GenerateApiContractSnapshots(string outputDirectory)
    {
        EnsureFile(ManifestPath, "Engineering Core V1 manifest");
        EnsureFile(DiagnosticsCatalogPath, "Engineering Core V1 diagnostics catalog");

        Directory.CreateDirectory(outputDirectory);

        var manifest = ReadJsonObject(ManifestPath);
        var diagnosticsCatalog = ReadJsonObject(DiagnosticsCatalogPath);

        var closedFormulaGates = manifest["closedFormulaGates"]?.AsArray() ?? [];
        var formulaGates = closedFormulaGates
            .Select(node => node?.GetValue<string>() ?? "")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(calculationId => new Dictionary<string, object?>
            {
                ["calculationId"] = calculationId,
                ["name"] = calculationId,
                ["status"] = "ClosedV1",
                ["priority"] = IsP1Gate(calculationId) ? "P1" : "P0",
                ["scope"] = "See docs/releases/EngineeringCoreV1Manifest.json and FormulaAuditMatrix for authoritative scope.",
                ["limitation"] = "ClosedV1 engineering formula gate with documented limitations; no exact EnergyPlus/StandardReference/ASHRAE 140 equivalence claim."
            })
            .ToArray();

        var statusSnapshot = new Dictionary<string, object?>
        {
            ["coreName"] = StringValue(manifest, "coreName"),
            ["version"] = StringValue(manifest, "version"),
            ["status"] = StringValue(manifest, "status"),
            ["formulaGatesClosed"] = BoolValue(manifest, "formulaGatesClosed"),
            ["weather8760GatesClosed"] = BoolValue(manifest, "weather8760GatesClosed"),
            ["annualHourly8760GateClosed"] = BoolValue(manifest, "annualHourly8760GateClosed"),
            ["successfulResultsMustNotContainErrorDiagnostics"] = BoolValue(manifest, "successfulResultsMustNotContainErrorDiagnostics"),
            ["formulaGates"] = formulaGates,
            ["explicitNonClaims"] = StringArray(manifest["explicitNonClaims"]),
            ["outOfScopeV1"] = StringArray(manifest["outOfScopeV1"]),
            ["plannedValidation"] = StringArray(manifest["plannedValidation"]),
            ["requiredAnnual8760Flags"] = StringArray(manifest["requiredAnnual8760Flags"]),
            ["documentationFiles"] = StringArray(manifest["documentationFiles"])
        };

        var diagnosticsSnapshot = new Dictionary<string, object?>
        {
            ["catalogName"] = StringValue(diagnosticsCatalog, "catalogName"),
            ["version"] = StringValue(diagnosticsCatalog, "version"),
            ["status"] = StringValue(diagnosticsCatalog, "status"),
            ["rules"] = diagnosticsCatalog["rules"],
            ["diagnostics"] = diagnosticsCatalog["diagnostics"]?.AsArray()
        };

        var statusJsonPath = Path.Combine(outputDirectory, "status.sample.json");
        var diagnosticsJsonPath = Path.Combine(outputDirectory, "diagnostics-catalog.sample.json");
        var httpPath = Path.Combine(outputDirectory, "engineering-core-v1.http");

        WriteJson(statusJsonPath, statusSnapshot);
        WriteJson(diagnosticsJsonPath, diagnosticsSnapshot);

        var httpContent =
            "@baseUrl = https://localhost:5001" + Environment.NewLine +
            Environment.NewLine +
            "### Engineering Core V1 status" + Environment.NewLine +
            "GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/status" + Environment.NewLine +
            "Accept: application/json" + Environment.NewLine +
            Environment.NewLine +
            "### Engineering Core V1 diagnostics catalog" + Environment.NewLine +
            "GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/diagnostics-catalog" + Environment.NewLine +
            "Accept: application/json" + Environment.NewLine;

        File.WriteAllText(httpPath, httpContent, Utf8NoBom());

        WriteSuccess("Engineering Core V1 API contract snapshots generated:");
        Console.WriteLine($"- {statusJsonPath}");
        Console.WriteLine($"- {diagnosticsJsonPath}");
        Console.WriteLine($"- {httpPath}");
        Console.WriteLine($"Formula gates: {formulaGates.Length}");
        Console.WriteLine($"Diagnostics: {diagnosticsCatalog["diagnostics"]?.AsArray().Count ?? 0}");

        return 0;
    }

    private static int GenerateReportContractSnapshots(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var documentationFiles = new[]
        {
            "docs/calculations/EngineeringCoreV1Scope.md",
            "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
            "docs/calculations/EnergyPlusAshrae140ValidationPlan.md"
        };

        var explicitNonClaims = ExplicitNonClaims();

        var outOfScopeV1 = new[]
        {
            "HVAC.LATENT_LOAD",
            "HVAC.MOISTURE_BALANCE",
            "Humidification/dehumidification conditions",
            "Detailed psychrometric supply-air treatment",
            "Detailed HVAC plant simulation"
        };

        var heatingDisclosure = new Dictionary<string, object?>
        {
            ["coreStatus"] = "ClosedV1",
            ["calculationScope"] = "Engineering-core v1 heating design-point report.",
            ["calculationMethod"] = "EngineeringCoreV1.DesignPointHeating",
            ["actualMethod"] = "EngineeringCoreV1.DesignPointHeating",
            ["warnings"] = new[]
            {
                "Heating report uses engineering design-point load calculation.",
                "Report does not claim full ISO 52016 node/matrix solver equivalence.",
                "Report does not claim exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence.",
                "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
            },
            ["assumptions"] = new[]
            {
                "Heating load is assembled from transmission and ventilation/infiltration components.",
                "Transmission uses steady-state U*A*?T component heat transfer.",
                "Ventilation and infiltration use sensible-only airflow heat transfer.",
                "Ground and adjacent boundaries are simplified engineering models when present."
            },
            ["explicitNonClaims"] = explicitNonClaims,
            ["outOfScopeV1"] = outOfScopeV1,
            ["documentationFiles"] = documentationFiles
        };

        var coolingDisclosure = new Dictionary<string, object?>
        {
            ["coreStatus"] = "ClosedV1",
            ["calculationScope"] = "Engineering-core v1 cooling design-point report.",
            ["calculationMethod"] = "EngineeringCoreV1.DesignPointCooling",
            ["actualMethod"] = "EngineeringCoreV1.DesignPointCooling",
            ["warnings"] = new[]
            {
                "Cooling report uses engineering design-point load calculation.",
                "Report does not claim full ISO 52016 node/matrix solver equivalence.",
                "Report does not claim exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence.",
                "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
            },
            ["assumptions"] = new[]
            {
                "Cooling load is assembled from transmission, ventilation, infiltration, solar and internal gain components.",
                "Window solar gains use simplified SHGC/shading based engineering model.",
                "Surface irradiance uses ISO52010-inspired solar geometry and isotropic sky transposition.",
                "Equipment selection, when requested, is capacity-margin based and does not model part-load curves."
            },
            ["explicitNonClaims"] = explicitNonClaims,
            ["outOfScopeV1"] = outOfScopeV1,
            ["documentationFiles"] = documentationFiles
        };

        var annualEnergyDisclosure = new Dictionary<string, object?>
        {
            ["coreStatus"] = "ClosedV1",
            ["calculationScope"] = "Engineering-core v1 hourly annual energy integration report.",
            ["calculationMethod"] = "TrueHourlySimulation",
            ["actualMethod"] = "EngineeringCoreV1.TrueHourly8760",
            ["warnings"] = new[]
            {
                "Annual energy is true hourly 8760 only when EnergyDataSource=TrueHourlySimulation, IsTrueHourly8760=true and HourlyRecordCount=8760.",
                "Monthly adapter, synthetic weather and deterministic short fixtures must not be presented as true hourly 8760 annual simulation.",
                "Report does not claim exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence."
            },
            ["assumptions"] = new[]
            {
                "Annual energy is calculated as sum of hourly W*h divided by 1000.",
                "Monthly totals are aggregated from hourly records.",
                "EPW and PVGIS import gates normalize weather to 8760 hourly records."
            },
            ["explicitNonClaims"] = explicitNonClaims,
            ["outOfScopeV1"] = outOfScopeV1,
            ["documentationFiles"] = documentationFiles
        };

        var heatingReport = new Dictionary<string, object?>
        {
            ["projectName"] = "Engineering Core V1 Sample",
            ["buildingName"] = "Heating Sample Building",
            ["calculationMethod"] = "EngineeringCoreV1.DesignPointHeating",
            ["generatedAtUtc"] = "2026-01-01T00:00:00Z",
            ["outdoorDesignTemperatureC"] = -10,
            ["indoorDesignTemperatureC"] = 20,
            ["roomsCount"] = 2,
            ["totalTransmissionLossW"] = 3200,
            ["totalVentilationLossW"] = 850,
            ["totalDesignHeatingLoadW"] = 4050,
            ["totalDesignHeatingLoadKw"] = 4.05,
            ["calculationDisclosure"] = heatingDisclosure,
            ["rooms"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["roomId"] = 101,
                    ["roomName"] = "Office 101",
                    ["transmissionHeatLossW"] = 1800,
                    ["ventilationHeatLossW"] = 450,
                    ["totalDesignHeatingLoadW"] = 2250
                },
                new Dictionary<string, object?>
                {
                    ["roomId"] = 102,
                    ["roomName"] = "Office 102",
                    ["transmissionHeatLossW"] = 1400,
                    ["ventilationHeatLossW"] = 400,
                    ["totalDesignHeatingLoadW"] = 1800
                }
            }
        };

        var coolingReport = new Dictionary<string, object?>
        {
            ["projectName"] = "Engineering Core V1 Sample",
            ["buildingName"] = "Cooling Sample Building",
            ["calculationMethod"] = "EngineeringCoreV1.DesignPointCooling",
            ["peakHourOfYear"] = 5200,
            ["generatedAtUtc"] = "2026-01-01T00:00:00Z",
            ["floorsCount"] = 1,
            ["roomsCount"] = 2,
            ["designReserveFactor"] = 1.15,
            ["designCapacityW"] = 6900,
            ["designCapacityKw"] = 6.9,
            ["coolingLoadW"] = 6000,
            ["coolingLoadKw"] = 6.0,
            ["calculationDisclosure"] = coolingDisclosure,
            ["floorSummaries"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["floorId"] = 1,
                    ["floorName"] = "Level 1",
                    ["roomsCount"] = 2,
                    ["coolingLoadW"] = 6000,
                    ["coolingLoadKw"] = 6.0
                }
            },
            ["rooms"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["roomId"] = 201,
                    ["roomName"] = "Meeting Room",
                    ["coolingLoadW"] = 3600,
                    ["coolingLoadKw"] = 3.6,
                    ["windowHeatGainW"] = 900,
                    ["wallHeatGainW"] = 700,
                    ["internalHeatGainW"] = 1200
                },
                new Dictionary<string, object?>
                {
                    ["roomId"] = 202,
                    ["roomName"] = "Open Office",
                    ["coolingLoadW"] = 2400,
                    ["coolingLoadKw"] = 2.4,
                    ["windowHeatGainW"] = 550,
                    ["wallHeatGainW"] = 500,
                    ["internalHeatGainW"] = 850
                }
            }
        };

        var annualEnergyReport = new Dictionary<string, object?>
        {
            ["projectName"] = "Engineering Core V1 Sample",
            ["buildingName"] = "Annual Energy Sample Building",
            ["energyDataSource"] = "TrueHourlySimulation",
            ["isTrueHourly8760"] = true,
            ["hourlyRecordCount"] = 8760,
            ["annualHeatingKwh"] = 18000,
            ["annualCoolingKwh"] = 6200,
            ["annualTotalKwh"] = 24200,
            ["euiKwhPerM2"] = 121,
            ["calculationDisclosure"] = annualEnergyDisclosure,
            ["requiredAnnual8760Flags"] = new[]
            {
                "EnergyDataSource = TrueHourlySimulation",
                "IsTrueHourly8760 = true",
                "HourlyRecordCount = 8760"
            }
        };

        var heatingPath = Path.Combine(outputDirectory, "heating-report.sample.json");
        var coolingPath = Path.Combine(outputDirectory, "cooling-report.sample.json");
        var annualPath = Path.Combine(outputDirectory, "annual-energy-disclosure.sample.json");

        WriteJson(heatingPath, heatingReport);
        WriteJson(coolingPath, coolingReport);
        WriteJson(annualPath, annualEnergyReport);

        WriteSuccess("Engineering Core V1 report contract snapshots generated:");
        Console.WriteLine($"- {heatingPath}");
        Console.WriteLine($"- {coolingPath}");
        Console.WriteLine($"- {annualPath}");

        return 0;
    }

    private static bool IsP1Gate(string calculationId) =>
        calculationId.Contains("DHW", StringComparison.Ordinal) ||
        calculationId.Contains("SYSTEM_ENERGY", StringComparison.Ordinal) ||
        calculationId.Contains("EQUIPMENT_SIZING", StringComparison.Ordinal) ||
        calculationId.Contains("GROUND", StringComparison.Ordinal) ||
        calculationId.Contains("ADJACENT", StringComparison.Ordinal);

    private static string[] ExplicitNonClaims() =>
    [
        "No exact StandardReference numerical equivalence claim.",
        "No exact EnergyPlus numerical equivalence claim.",
        "No ASHRAE 140 / BESTEST-style validation anchor coverage claim.",
        "No full ISO 52016 node/matrix solver equivalence claim.",
        "No full ISO 52010 climate conversion equivalence claim.",
        "No full ISO 13370 implementation claim.",
        "No full EN 15316 generation/distribution/storage/emission chain claim.",
        "No full coupled multi-zone heat-balance simulation claim.",
        "No latent/moisture/humidity calculation claim."
    ];

    private static string? ReadOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        }

        return null;
    }

    private static JsonObject ReadJsonObject(string path)
    {
        EnsureFile(path, "JSON input file");
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }

    private static string StringValue(JsonObject source, string property) =>
        source[property]?.GetValue<string>() ?? "";

    private static bool BoolValue(JsonObject source, string property) =>
        source[property]?.GetValue<bool>() ?? false;

    private static string[] StringArray(JsonNode? node)
    {
        if (node is not JsonArray array)
            return [];

        return array
            .Select(item => item?.GetValue<string>() ?? "")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static void EnsureFile(string path, string description)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{description} not found: {path}", path);
    }

    private static void WriteJson(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(path, json + Environment.NewLine, Utf8NoBom());
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private static Encoding Utf8NoBom() =>
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
