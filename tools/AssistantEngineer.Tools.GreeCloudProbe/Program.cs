using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class Program
{
    private const string StageName = "GREE-ALICE-02";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(static arg => arg is "-h" or "--help" or "help"))
            {
                PrintHelp();
                return 0;
            }

            var options = ProbeOptions.Parse(args);
            var report = ProbeReport.Create(options, DateTimeOffset.UtcNow);

            Directory.CreateDirectory(options.OutputDirectory);

            var outputPath = Path.Combine(
                options.OutputDirectory,
                $"gree-cloud-probe-{report.TimestampUtc:yyyyMMdd-HHmmss}.json");

            File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));

            PrintSummary(options, report, outputPath);

            return 0;
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
        Console.WriteLine("AssistantEngineer Gree Cloud probe scaffold");
        Console.WriteLine();
        Console.WriteLine("This stage validates probe configuration and masked diagnostic output only.");
        Console.WriteLine("It does not call Gree Cloud yet.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repo-root <path>");
        Console.WriteLine("  --region <name>");
        Console.WriteLine("  --username <value>");
        Console.WriteLine("  --password <value>");
        Console.WriteLine("  --output-dir <path>");
        Console.WriteLine("  --timeout-seconds <number>");
        Console.WriteLine("  --save-raw-response");
        Console.WriteLine("  --no-mask-secrets");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");
        Console.WriteLine("  GREE_ALICE_GREE_REGION");
        Console.WriteLine("  GREE_ALICE_GREE_USERNAME");
        Console.WriteLine("  GREE_ALICE_GREE_PASSWORD");
        Console.WriteLine("  GREE_ALICE_OUTPUT_DIR");
        Console.WriteLine("  GREE_ALICE_TIMEOUT_SECONDS");
        Console.WriteLine("  GREE_ALICE_SAVE_RAW_RESPONSE");
        Console.WriteLine("  GREE_ALICE_MASK_SECRETS");
    }

    private static void PrintSummary(ProbeOptions options, ProbeReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree Cloud probe scaffold");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine("Cloud call: not executed in this stage");
        Console.WriteLine($"Region: {DisplayValue(options.Region)}");
        Console.WriteLine($"Username: {MaskValue(options.Username, options.MaskSecrets)}");
        Console.WriteLine($"Password provided: {ToYesNo(!string.IsNullOrWhiteSpace(options.Password))}");
        Console.WriteLine($"Timeout seconds: {options.TimeoutSeconds}");
        Console.WriteLine($"Save raw response: {ToYesNo(options.SaveRawResponse)}");
        Console.WriteLine($"Mask secrets: {ToYesNo(options.MaskSecrets)}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Homes: {report.Summary.HomesCount}");
        Console.WriteLine($"  Rooms: {report.Summary.RoomsCount}");
        Console.WriteLine($"  Devices: {report.Summary.DevicesCount}");
        Console.WriteLine($"  Split candidates: {report.Summary.SplitCandidatesCount}");
        Console.WriteLine($"  VRF gateway candidates: {report.Summary.VrfGatewayCandidatesCount}");
        Console.WriteLine($"  VRF child-unit candidates: {report.Summary.VrfChildUnitCandidatesCount}");
        Console.WriteLine($"  Unknown devices: {report.Summary.UnknownDevicesCount}");
        Console.WriteLine();
        Console.WriteLine("Next step: implement Gree Cloud login and discovery in GREE-ALICE-03.");
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "<not set>" : value;

    private static string MaskValue(string? value, bool maskSecrets)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "<not set>";

        if (!maskSecrets)
            return value;

        if (value.Length <= 4)
            return new string('*', value.Length);

        if (value.Length <= 8)
            return value[..2] + "..." + value[^2..];

        return value[..4] + "..." + value[^4..];
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record ProbeOptions(
        string RepositoryRoot,
        string Region,
        string Username,
        string Password,
        string OutputDirectory,
        int TimeoutSeconds,
        bool SaveRawResponse,
        bool MaskSecrets)
    {
        public static ProbeOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var region = GetValue(values, "region", "GREE_ALICE_GREE_REGION") ?? string.Empty;
            var username = GetValue(values, "username", "GREE_ALICE_GREE_USERNAME") ?? string.Empty;
            var password = GetValue(values, "password", "GREE_ALICE_GREE_PASSWORD") ?? string.Empty;
            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");

            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "probe");

            outputDir = Path.GetFullPath(outputDir);

            var timeoutRaw = GetValue(values, "timeout-seconds", "GREE_ALICE_TIMEOUT_SECONDS");
            var timeoutSeconds = ParseInt(timeoutRaw, 30, 1, 300, "timeout-seconds");

            var saveRawResponse = ParseBool(
                GetValue(values, "save-raw-response", "GREE_ALICE_SAVE_RAW_RESPONSE"),
                values.ContainsKey("save-raw-response"));

            var maskSecrets = values.ContainsKey("no-mask-secrets")
                ? false
                : ParseBool(GetValue(values, "mask-secrets", "GREE_ALICE_MASK_SECRETS"), defaultValue: true);

            return new ProbeOptions(
                repoRoot,
                region.Trim(),
                username.Trim(),
                password,
                outputDir,
                timeoutSeconds,
                saveRawResponse,
                maskSecrets);
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                if (!arg.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unexpected argument: {arg}");

                var keyValue = arg[2..];
                var equalsIndex = keyValue.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    var key = keyValue[..equalsIndex];
                    var value = keyValue[(equalsIndex + 1)..];
                    result[key] = value;
                    continue;
                }

                if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    result[keyValue] = args[index + 1];
                    index++;
                    continue;
                }

                result[keyValue] = null;
            }

            return result;
        }

        private static string? GetValue(
            IReadOnlyDictionary<string, string?> values,
            string argumentName,
            string? environmentVariableName)
        {
            if (values.TryGetValue(argumentName, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            return environmentVariableName is null
                ? null
                : Environment.GetEnvironmentVariable(environmentVariableName);
        }

        private static int ParseInt(string? raw, int defaultValue, int min, int max, string name)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            if (!int.TryParse(raw, out var value))
                throw new ArgumentException($"{name} must be an integer.");

            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {min} and {max}.");

            return value;
        }

        private static bool ParseBool(string? raw, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            return raw.Trim().ToLowerInvariant() switch
            {
                "1" or "true" or "yes" or "y" => true,
                "0" or "false" or "no" or "n" => false,
                _ => throw new ArgumentException($"Boolean value is invalid: {raw}")
            };
        }

        private static string ResolveRepositoryRoot()
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
    }

    private sealed record ProbeReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        ProbeInputs Inputs,
        ProbeSummary Summary,
        IReadOnlyList<ProbeDevice> Devices,
        IReadOnlyList<string> Notes)
    {
        public static ProbeReport Create(ProbeOptions options, DateTimeOffset timestampUtc)
        {
            return new ProbeReport(
                StageName,
                "scaffold",
                timestampUtc,
                new ProbeInputs(
                    options.Region,
                    MaskValue(options.Username, options.MaskSecrets),
                    !string.IsNullOrWhiteSpace(options.Password),
                    options.TimeoutSeconds,
                    options.SaveRawResponse,
                    options.MaskSecrets),
                new ProbeSummary(
                    HomesCount: 0,
                    RoomsCount: 0,
                    DevicesCount: 0,
                    SplitCandidatesCount: 0,
                    VrfGatewayCandidatesCount: 0,
                    VrfChildUnitCandidatesCount: 0,
                    OfflineDevicesCount: 0,
                    UnknownDevicesCount: 0),
                Array.Empty<ProbeDevice>(),
                new[]
                {
                    "GREE-ALICE-02 writes a scaffold report only.",
                    "Gree Cloud login and discovery are intentionally not executed in this stage.",
                    "Runtime API, Telegram bot, deployment files, and migrations are not touched."
                });
        }
    }

    private sealed record ProbeInputs(
        string Region,
        string Username,
        bool PasswordProvided,
        int TimeoutSeconds,
        bool SaveRawResponse,
        bool MaskSecrets);

    private sealed record ProbeSummary(
        int HomesCount,
        int RoomsCount,
        int DevicesCount,
        int SplitCandidatesCount,
        int VrfGatewayCandidatesCount,
        int VrfChildUnitCandidatesCount,
        int OfflineDevicesCount,
        int UnknownDevicesCount);

    private sealed record ProbeDevice(
        string? HomeId,
        string? HomeName,
        string? RoomId,
        string? RoomName,
        string? DeviceId,
        string? DeviceName,
        string? DeviceType,
        string? DeviceModel,
        string? Mac,
        string? ParentId,
        string? ParentMac,
        string? ChildId,
        string? ChildMac,
        bool? Online,
        IReadOnlyList<string> RawCapabilityNames);
}
