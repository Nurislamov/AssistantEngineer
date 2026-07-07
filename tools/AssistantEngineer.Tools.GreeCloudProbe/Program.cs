using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class Program
{
    private const string StageName = "GREE-ALICE-07";

    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Any(static arg => arg is "-h" or "--help" or "help"))
        {
            PrintHelp();
            return 0;
        }

        try
        {
            if (args.Any(static arg => arg.Equals("--probe-live-status", StringComparison.OrdinalIgnoreCase)))
                return await LiveStatusProbeCommand.RunAsync(args);
            if (args.Any(static arg => arg.Equals("--summarize-capture", StringComparison.OrdinalIgnoreCase)))
                return CaptureSummaryCommand.Run(args);

            if (args.Any(static arg => arg.Equals("--normalize-latest-report", StringComparison.OrdinalIgnoreCase)))
                return NormalizeReportCommand.Run(args);

            var options = ProbeOptions.Parse(args);
            Directory.CreateDirectory(options.OutputDirectory);

            var timestampUtc = DateTimeOffset.UtcNow;
            ProbeReport report;

            if (options.ConfigurationOnly || !options.HasCredentials)
            {
                report = ProbeReport.ConfigurationOnly(options, timestampUtc);
            }
            else
            {
                report = await ProbeReport.RunCloudDiscoveryAsync(options, timestampUtc);
            }

            var outputPath = Path.Combine(
                options.OutputDirectory,
                $"gree-cloud-probe-{timestampUtc:yyyyMMdd-HHmmss}.json");

            File.WriteAllText(outputPath, JsonSerializer.Serialize(report, ReportJsonOptions));
            PrintSummary(options, report, outputPath);

            return report.Errors.Count == 0 ? 0 : 2;
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
        Console.WriteLine("AssistantEngineer Gree Cloud probe");
        Console.WriteLine();
        Console.WriteLine("This tool validates Gree+ Cloud login, homes, rooms, and device discovery.");
        Console.WriteLine("Sensitive values are masked by default.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repo-root <path>");
        Console.WriteLine("  --region <name>");
        Console.WriteLine("  --server-url <url>");
        Console.WriteLine("  --username <value>");
        Console.WriteLine("  --password <value>");
        Console.WriteLine("  --output-dir <path>");
        Console.WriteLine("  --timeout-seconds <number>");
        Console.WriteLine("  --save-raw-response");
        Console.WriteLine("  --no-mask-secrets");
        Console.WriteLine("  --configuration-only");
        Console.WriteLine("  --normalize-latest-report");
        Console.WriteLine("  --summarize-capture");
        Console.WriteLine("  --capture-input <path>");
        Console.WriteLine("  --probe-live-status");
        Console.WriteLine("  --max-attempts <number>");
        Console.WriteLine("  --input-report <path>");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");
        Console.WriteLine("  GREE_ALICE_GREE_REGION");
        Console.WriteLine("  GREE_ALICE_GREE_SERVER_URL");
        Console.WriteLine("  GREE_ALICE_GREE_USERNAME");
        Console.WriteLine("  GREE_ALICE_GREE_PASSWORD");
        Console.WriteLine("  GREE_ALICE_OUTPUT_DIR");
        Console.WriteLine("  GREE_ALICE_TIMEOUT_SECONDS");
        Console.WriteLine("  GREE_ALICE_SAVE_RAW_RESPONSE");
        Console.WriteLine("  GREE_ALICE_MASK_SECRETS");
    }

    private static void PrintSummary(ProbeOptions options, ProbeReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree Cloud probe");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Region: {DisplayValue(options.Region)}");
        Console.WriteLine($"Server URL: {DisplayValue(options.ServerUrl)}");
        Console.WriteLine($"Username: {MaskValue(options.Username, options.MaskSecrets)}");
        Console.WriteLine($"Password provided: {ToYesNo(!string.IsNullOrWhiteSpace(options.Password))}");
        Console.WriteLine($"Timeout seconds: {options.TimeoutSeconds}");
        Console.WriteLine($"Save raw response: {ToYesNo(options.SaveRawResponse)}");
        Console.WriteLine($"Mask secrets: {ToYesNo(options.MaskSecrets)}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Cloud:");
        Console.WriteLine($"  Login attempted: {ToYesNo(report.Auth.LoginAttempted)}");
        Console.WriteLine($"  Login succeeded: {ToYesNo(report.Auth.LoginSucceeded)}");
        Console.WriteLine($"  Token provided: {ToYesNo(report.Auth.TokenProvided)}");
        Console.WriteLine();

        Console.WriteLine("Summary:");
        Console.WriteLine($"  Homes: {report.Summary.HomesCount}");
        Console.WriteLine($"  Rooms: {report.Summary.RoomsCount}");
        Console.WriteLine($"  Devices: {report.Summary.DevicesCount}");
        Console.WriteLine($"  Split candidates: {report.Summary.SplitCandidatesCount}");
        Console.WriteLine($"  VRF gateway candidates: {report.Summary.VrfGatewayCandidatesCount}");
        Console.WriteLine($"  VRF child-unit candidates: {report.Summary.VrfChildUnitCandidatesCount}");
        Console.WriteLine($"  Offline devices: {report.Summary.OfflineDevicesCount}");
        Console.WriteLine($"  Unknown devices: {report.Summary.UnknownDevicesCount}");

        if (report.Errors.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Errors:");
            Console.ResetColor();

            foreach (var error in report.Errors)
                Console.WriteLine($"  - {error.Code}: {error.Message}");
        }

        Console.WriteLine();

        if (report.Mode == "configuration-only")
        {
            Console.WriteLine("Next step: set GREE_ALICE_GREE_USERNAME and GREE_ALICE_GREE_PASSWORD, then run the tool without --configuration-only.");
        }
        else if (report.Errors.Count == 0)
        {
            Console.WriteLine("Next step: inspect the masked JSON report and confirm whether split/VRF devices are visible.");
        }
        else
        {
            Console.WriteLine("Next step: inspect the masked JSON report, then adjust region/server-url or credentials.");
        }
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

    private static string MaskLongValue(string? value, bool maskSecrets)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "<not set>";

        if (!maskSecrets)
            return value;

        if (value.Length <= 10)
            return new string('*', value.Length);

        return value[..4] + "..." + value[^4..];
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private static string SanitizedError(Exception exception, ProbeOptions options)
    {
        var message = exception.Message;

        if (!string.IsNullOrWhiteSpace(options.Username))
            message = message.Replace(options.Username, MaskValue(options.Username, maskSecrets: true), StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(options.Password))
            message = message.Replace(options.Password, "<password>", StringComparison.OrdinalIgnoreCase);

        return message;
    }

    private sealed record ProbeOptions(
        string RepositoryRoot,
        string Region,
        string ServerUrl,
        string Username,
        string Password,
        string OutputDirectory,
        int TimeoutSeconds,
        bool SaveRawResponse,
        bool MaskSecrets,
        bool ConfigurationOnly)
    {
        private static readonly IReadOnlyDictionary<string, string> ServerAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["europe"] = "https://eugrih.gree.com",
                ["eu"] = "https://eugrih.gree.com",
                ["eugrih"] = "https://eugrih.gree.com",

                ["eastsouthasia"] = "https://hkgrih.gree.com",
                ["southeastasia"] = "https://hkgrih.gree.com",
                ["asia"] = "https://hkgrih.gree.com",
                ["hongkong"] = "https://hkgrih.gree.com",
                ["hkgrih"] = "https://hkgrih.gree.com",

                ["northamerican"] = "https://nagrih.gree.com",
                ["northamerica"] = "https://nagrih.gree.com",
                ["na"] = "https://nagrih.gree.com",
                ["nagrih"] = "https://nagrih.gree.com",

                ["southamerican"] = "https://sagrih.gree.com",
                ["southamerica"] = "https://sagrih.gree.com",
                ["sa"] = "https://sagrih.gree.com",
                ["sagrih"] = "https://sagrih.gree.com",

                ["chinamainland"] = "https://grih.gree.com",
                ["china"] = "https://grih.gree.com",
                ["grih"] = "https://grih.gree.com",

                ["india"] = "https://ingrih.gree.com",
                ["ingrih"] = "https://ingrih.gree.com",

                ["middleeast"] = "https://megrih.gree.com",
                ["middleeastserver"] = "https://megrih.gree.com",
                ["me"] = "https://megrih.gree.com",
                ["megrih"] = "https://megrih.gree.com",
                // In the Gree+ app, Uzbekistan can be displayed as "Ouzbékistan"; validated account/server mapping for this project is East South Asia.
                ["ouzbekistan"] = "https://hkgrih.gree.com",
                ["ouzbékistan"] = "https://hkgrih.gree.com",
                ["uzbekistan"] = "https://hkgrih.gree.com",
                ["узбекистан"] = "https://hkgrih.gree.com",

                ["australia"] = "https://augrih.gree.com",
                ["au"] = "https://augrih.gree.com",
                ["augrih"] = "https://augrih.gree.com",

                ["russianserver"] = "https://rugrih.gree.com",
                ["russia"] = "https://rugrih.gree.com",
                ["ru"] = "https://rugrih.gree.com",
                ["rugrih"] = "https://rugrih.gree.com"
            };

        public bool HasCredentials =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        public static ProbeOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var region = GetValue(values, "region", "GREE_ALICE_GREE_REGION") ?? "Ouzbekistan";
            var serverUrl = GetValue(values, "server-url", "GREE_ALICE_GREE_SERVER_URL") ?? ResolveServerUrl(region);
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

            var configurationOnly = values.ContainsKey("configuration-only");

            return new ProbeOptions(
                repoRoot,
                region.Trim(),
                NormalizeServerUrl(serverUrl),
                username.Trim(),
                password,
                outputDir,
                timeoutSeconds,
                saveRawResponse,
                maskSecrets,
                configurationOnly);
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

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
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

        private static string ResolveServerUrl(string region)
        {
            if (Uri.TryCreate(region, UriKind.Absolute, out var regionAsUrl) &&
                (regionAsUrl.Scheme == Uri.UriSchemeHttps || regionAsUrl.Scheme == Uri.UriSchemeHttp))
            {
                return NormalizeServerUrl(region);
            }

            var normalized = NormalizeRegionKey(region);
            if (ServerAliases.TryGetValue(normalized, out var serverUrl))
                return serverUrl;

            throw new ArgumentException($"Unknown Gree Cloud region '{region}'. Use --server-url or GREE_ALICE_GREE_SERVER_URL.");
        }

        private static string NormalizeRegionKey(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var ch in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                    builder.Append(ch);
            }

            return builder.ToString();
        }

        private static string NormalizeServerUrl(string value)
        {
            var trimmed = value.Trim().TrimEnd('/');
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid server URL: {value}");

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                throw new ArgumentException($"Server URL must be http or https: {value}");

            return trimmed;
        }
    }

    private sealed record ProbeReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        ProbeInputs Inputs,
        ProbeAuth Auth,
        ProbeSummary Summary,
        IReadOnlyList<ProbeHome> Homes,
        IReadOnlyList<ProbeDevice> Devices,
        IReadOnlyList<EndpointTrace> EndpointTraces,
        IReadOnlyList<ProbeError> Errors,
        IReadOnlyList<string> Notes)
    {
        public static ProbeReport ConfigurationOnly(ProbeOptions options, DateTimeOffset timestampUtc)
        {
            return new ProbeReport(
                StageName,
                "configuration-only",
                timestampUtc,
                ProbeInputs.FromOptions(options),
                new ProbeAuth(LoginAttempted: false, LoginSucceeded: false, UserId: null, TokenProvided: false, TokenMasked: null),
                ProbeSummary.Empty,
                Array.Empty<ProbeHome>(),
                Array.Empty<ProbeDevice>(),
                Array.Empty<EndpointTrace>(),
                Array.Empty<ProbeError>(),
                new[]
                {
                    "Cloud login and discovery were not executed.",
                    "Set credentials and run without --configuration-only to probe Gree+ Cloud.",
                    "Runtime API, Telegram bot, deployment files, and migrations are not touched."
                });
        }

        public static async Task<ProbeReport> RunCloudDiscoveryAsync(ProbeOptions options, DateTimeOffset timestampUtc)
        {
            var client = new GreeCloudClient(options);

            try
            {
                var discovery = await client.DiscoverAsync();
                var summary = ProbeSummary.From(discovery.Homes, discovery.Devices);

                return new ProbeReport(
                    StageName,
                    "cloud-discovery",
                    timestampUtc,
                    ProbeInputs.FromOptions(options),
                    new ProbeAuth(
                        LoginAttempted: true,
                        LoginSucceeded: true,
                        UserId: discovery.UserId,
                        TokenProvided: !string.IsNullOrWhiteSpace(discovery.Token),
                        TokenMasked: MaskLongValue(discovery.Token, options.MaskSecrets)),
                    summary,
                    discovery.Homes,
                    discovery.Devices,
                    discovery.EndpointTraces,
                    Array.Empty<ProbeError>(),
                    new[]
                    {
                        "Gree+ Cloud login completed.",
                        "Homes and devices were read through cloud discovery endpoints.",
                        "Secrets are masked by default in console output and report artifacts."
                    });
            }
            catch (Exception exception)
            {
                return new ProbeReport(
                    StageName,
                    "cloud-discovery",
                    timestampUtc,
                    ProbeInputs.FromOptions(options),
                    new ProbeAuth(LoginAttempted: true, LoginSucceeded: false, UserId: null, TokenProvided: false, TokenMasked: null),
                    ProbeSummary.Empty,
                    Array.Empty<ProbeHome>(),
                    Array.Empty<ProbeDevice>(),
                    client.EndpointTraces,
                    new[]
                    {
                        new ProbeError("GREE_CLOUD_DISCOVERY_FAILED", SanitizedError(exception, options))
                    },
                    new[]
                    {
                        "Gree+ Cloud discovery failed.",
                        "Check credentials, region/server URL, network access, and whether the account can log in through the official Gree+ app."
                    });
            }
        }
    }

    private sealed record ProbeInputs(
        string Region,
        string ServerUrl,
        string Username,
        bool PasswordProvided,
        int TimeoutSeconds,
        bool SaveRawResponse,
        bool MaskSecrets)
    {
        public static ProbeInputs FromOptions(ProbeOptions options)
        {
            return new ProbeInputs(
                options.Region,
                options.ServerUrl,
                MaskValue(options.Username, options.MaskSecrets),
                !string.IsNullOrWhiteSpace(options.Password),
                options.TimeoutSeconds,
                options.SaveRawResponse,
                options.MaskSecrets);
        }
    }

    private sealed record ProbeAuth(
        bool LoginAttempted,
        bool LoginSucceeded,
        string? UserId,
        bool TokenProvided,
        string? TokenMasked);

    private sealed record ProbeSummary(
        int HomesCount,
        int RoomsCount,
        int DevicesCount,
        int SplitCandidatesCount,
        int VrfGatewayCandidatesCount,
        int VrfChildUnitCandidatesCount,
        int OfflineDevicesCount,
        int UnknownDevicesCount)
    {
        public static ProbeSummary Empty { get; } = new(
            HomesCount: 0,
            RoomsCount: 0,
            DevicesCount: 0,
            SplitCandidatesCount: 0,
            VrfGatewayCandidatesCount: 0,
            VrfChildUnitCandidatesCount: 0,
            OfflineDevicesCount: 0,
            UnknownDevicesCount: 0);

        public static ProbeSummary From(IReadOnlyList<ProbeHome> homes, IReadOnlyList<ProbeDevice> devices)
        {
            var roomKeys = devices
                .Where(static device => !string.IsNullOrWhiteSpace(device.RoomId) || !string.IsNullOrWhiteSpace(device.RoomName))
                .Select(static device => $"{device.HomeId}|{device.RoomId}|{device.RoomName}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return new ProbeSummary(
                HomesCount: homes.Count,
                RoomsCount: roomKeys,
                DevicesCount: devices.Count,
                SplitCandidatesCount: devices.Count(static device => device.Classification == "split-candidate"),
                VrfGatewayCandidatesCount: devices.Count(static device => device.Classification == "vrf-gateway-candidate"),
                VrfChildUnitCandidatesCount: devices.Count(static device => device.Classification == "vrf-child-unit-candidate"),
                OfflineDevicesCount: devices.Count(static device => device.Online == false),
                UnknownDevicesCount: devices.Count(static device => device.Classification == "unknown"));
        }
    }

    private sealed record ProbeHome(
        string? HomeId,
        string? HomeName);

    private sealed record ProbeDevice(
        string? HomeId,
        string? HomeName,
        string? RoomId,
        string? RoomName,
        string? DeviceId,
        string? DeviceName,
        string? DeviceType,
        string? DeviceModel,
        string? Version,
        string? Mac,
        string? ParentId,
        string? ParentMac,
        string? ChildId,
        string? ChildMac,
        bool? Online,
        bool KeyProvided,
        string? KeyMasked,
        string Classification,
        IReadOnlyList<string> RawFieldNames,
        IReadOnlyDictionary<string, object?> SafeRawProperties);

    private sealed record EndpointTrace(
        string Endpoint,
        int? HttpStatus,
        bool HttpSucceeded,
        bool EncryptedResponseProvided,
        string? RawHttpResponsePreview,
        string? Error);

    private sealed record ProbeError(
        string Code,
        string Message);

    private sealed record CloudDiscovery(
        string? UserId,
        string? Token,
        IReadOnlyList<ProbeHome> Homes,
        IReadOnlyList<ProbeDevice> Devices,
        IReadOnlyList<EndpointTrace> EndpointTraces);

    private sealed class GreeCloudClient
    {
        private const string AppId = "4920681951525131286";
        private const string AppHash = "0fa513124aa97781d1f3f40d61ca1a89";
        private const string AesKeyText = "#G$&^jgfujy6ujxt";
        private const string Gaen1Header = "5ac2bdf935bcca70";

        private readonly ProbeOptions options;
        private readonly HttpClient httpClient;
        private readonly List<EndpointTrace> endpointTraces = new();

        private string? userId;
        private string? token;

        public GreeCloudClient(ProbeOptions options)
        {
            this.options = options;
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.ServerUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        }

        public IReadOnlyList<EndpointTrace> EndpointTraces => endpointTraces;

        public async Task<CloudDiscovery> DiscoverAsync()
        {
            await LoginAsync();
            var homes = await GetHomesAsync();

            var devices = new List<ProbeDevice>();
            foreach (var home in homes)
            {
                var homeDevices = await GetDevicesAsync(home);
                devices.AddRange(homeDevices);
            }

            return new CloudDiscovery(userId, token, homes, devices, endpointTraces);
        }

        private async Task LoginAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var t = FormatCloudTimestamp(now);
            var h = Md5(Md5(options.Password) + options.Password);
            var psw = Md5(h + t);

            var payload = new Dictionary<string, object?>
            {
                ["psw"] = psw,
                ["t"] = t,
                ["user"] = options.Username
            };

            var decrypted = await SendEncryptedRequestAsync("/App/UserLoginV2", payload, now, new[] { "user", "psw", "t" });

            using var document = JsonDocument.Parse(decrypted);
            userId = JsonHelpers.TryGetStringRecursive(document.RootElement, "uid", "userId", "userid");
            token = JsonHelpers.TryGetStringRecursive(document.RootElement, "token");

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                var fields = JsonHelpers.GetRootFieldNames(document.RootElement);
                var preview = JsonHelpers.CreateSafePreview(decrypted, options.MaskSecrets, maxLength: 1000);
                throw new InvalidOperationException($"Login response did not contain uid/token. Root fields: {fields}. Decrypted preview: {preview}");
            }
        }

        private async Task<IReadOnlyList<ProbeHome>> GetHomesAsync()
        {
            EnsureLoggedIn();

            var now = DateTimeOffset.UtcNow;
            var payload = new Dictionary<string, object?>
            {
                ["token"] = token,
                ["uid"] = userId
            };

            var decrypted = await SendEncryptedRequestAsync("/App/GetHomes", payload, now, new[] { "token", "uid" });

            using var document = JsonDocument.Parse(decrypted);
            if (!JsonHelpers.TryGetArrayRecursive(document.RootElement, out var homesArray, "home", "homes"))
                return Array.Empty<ProbeHome>();

            var homes = new List<ProbeHome>();
            foreach (var home in homesArray.EnumerateArray())
            {
                homes.Add(new ProbeHome(
                    JsonHelpers.TryGetString(home, "id", "homeId", "hid"),
                    JsonHelpers.TryGetString(home, "name", "homeName")));
            }

            return homes;
        }

        private async Task<IReadOnlyList<ProbeDevice>> GetDevicesAsync(ProbeHome home)
        {
            EnsureLoggedIn();

            if (string.IsNullOrWhiteSpace(home.HomeId))
                return Array.Empty<ProbeDevice>();

            var now = DateTimeOffset.UtcNow;
            var homeIdPayload = long.TryParse(home.HomeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHomeId)
                ? parsedHomeId
                : (object)home.HomeId;

            var payload = new Dictionary<string, object?>
            {
                ["token"] = token,
                ["homeId"] = homeIdPayload,
                ["uid"] = userId
            };

            var decrypted = await SendEncryptedRequestAsync("/App/GetDevsInRoomsOfHomeV2", payload, now, new[] { "token", "uid", "homeId" });

            using var document = JsonDocument.Parse(decrypted);
            var devices = new List<ProbeDevice>();

            if (JsonHelpers.TryGetArrayRecursive(document.RootElement, out var roomsArray, "rooms", "room"))
            {
                foreach (var room in roomsArray.EnumerateArray())
                {
                    if (!JsonHelpers.TryGetArray(room, out var roomDevices, "devs", "devices"))
                        continue;

                    foreach (var device in roomDevices.EnumerateArray())
                        devices.Add(CreateDevice(home, room, device));
                }

                return devices;
            }

            if (JsonHelpers.TryGetArrayRecursive(document.RootElement, out var devicesArray, "devs", "devices"))
            {
                foreach (var device in devicesArray.EnumerateArray())
                    devices.Add(CreateDevice(home, default, device));
            }

            return devices;
        }

        private ProbeDevice CreateDevice(ProbeHome home, JsonElement room, JsonElement device)
        {
            var rawFieldNames = JsonHelpers.GetPropertyNames(device);
            var roomId = room.ValueKind == JsonValueKind.Object
                ? JsonHelpers.TryGetString(room, "id", "roomId", "rid")
                : null;
            var roomName = room.ValueKind == JsonValueKind.Object
                ? JsonHelpers.TryGetString(room, "name", "roomName")
                : null;

            var mac = JsonHelpers.TryGetString(device, "mac", "deviceMac");
            var parentMac = JsonHelpers.TryGetString(device, "parentMac", "pmac");
            var childMac = JsonHelpers.TryGetString(device, "childMac", "cmac");
            var key = JsonHelpers.TryGetString(device, "key", "aesKey", "secretKey");
            var online = JsonHelpers.TryGetBool(device, "online", "isOnline", "status");

            var probeDevice = new ProbeDevice(
                HomeId: home.HomeId,
                HomeName: home.HomeName,
                RoomId: roomId,
                RoomName: roomName,
                DeviceId: JsonHelpers.TryGetString(device, "id", "devId", "deviceId", "did"),
                DeviceName: JsonHelpers.TryGetString(device, "name", "devName", "deviceName"),
                DeviceType: JsonHelpers.TryGetString(device, "type", "devType", "deviceType", "category"),
                DeviceModel: JsonHelpers.TryGetString(device, "model", "devModel"),
                Version: JsonHelpers.TryGetString(device, "ver", "version"),
                Mac: MaskLongValue(mac, options.MaskSecrets),
                ParentId: JsonHelpers.TryGetString(device, "parentId", "pid"),
                ParentMac: MaskLongValue(parentMac, options.MaskSecrets),
                ChildId: JsonHelpers.TryGetString(device, "childId", "cid"),
                ChildMac: MaskLongValue(childMac, options.MaskSecrets),
                Online: online,
                KeyProvided: !string.IsNullOrWhiteSpace(key),
                KeyMasked: string.IsNullOrWhiteSpace(key) ? null : MaskLongValue(key, options.MaskSecrets),
                Classification: "unknown",
                RawFieldNames: rawFieldNames,
                SafeRawProperties: JsonHelpers.CreateSafePropertyMap(device, options.MaskSecrets));

            return probeDevice with { Classification = ClassifyDevice(probeDevice) };
        }

        private static string ClassifyDevice(ProbeDevice device)
        {
            var haystack = string.Join(
                " ",
                new[]
                {
                    device.DeviceName,
                    device.DeviceType,
                    device.DeviceModel,
                    device.Version,
                    device.ParentId,
                    device.ParentMac,
                    device.ChildId,
                    device.ChildMac
                }.Where(static value => !string.IsNullOrWhiteSpace(value))
                .Concat(device.RawFieldNames))
                .ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(device.ParentId) ||
                !string.IsNullOrWhiteSpace(device.ParentMac) ||
                !string.IsNullOrWhiteSpace(device.ChildId) ||
                !string.IsNullOrWhiteSpace(device.ChildMac))
            {
                return "vrf-child-unit-candidate";
            }

            if (haystack.Contains("vrf", StringComparison.Ordinal) ||
                haystack.Contains("gmv", StringComparison.Ordinal) ||
                haystack.Contains("gateway", StringComparison.Ordinal) ||
                haystack.Contains("multi", StringComparison.Ordinal) ||
                haystack.Contains("commercial", StringComparison.Ordinal))
            {
                return "vrf-gateway-candidate";
            }

            if (device.KeyProvided ||
                haystack.Contains("split", StringComparison.Ordinal) ||
                haystack.Contains("air", StringComparison.Ordinal) ||
                haystack.Contains("conditioner", StringComparison.Ordinal) ||
                haystack.Contains("climate", StringComparison.Ordinal) ||
                haystack.Contains("gwh", StringComparison.Ordinal))
            {
                return "split-candidate";
            }

            return "unknown";
        }

        private async Task<string> SendEncryptedRequestAsync(
            string endpoint,
            IReadOnlyDictionary<string, object?> payload,
            DateTimeOffset now,
            IReadOnlyList<string> hashProps)
        {
            var requestBody = PrepareBody(payload, now, hashProps);
            var jsonBody = JsonSerializer.Serialize(requestBody, CompactJsonOptions);
            var encryptedBody = Convert.ToBase64String(Encrypt(jsonBody));

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(encryptedBody, Encoding.UTF8)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Headers.TryAddWithoutValidation("Gaen1", Gaen1Header);
            request.Headers.TryAddWithoutValidation("Charset", "utf-8");

            try
            {
                using var response = await httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                var encryptedResponseProvided = false;
                string? rawPreview = null;

                if (options.SaveRawResponse)
                    rawPreview = Truncate(responseText, 2000);

                if (!response.IsSuccessStatusCode)
                {
                    endpointTraces.Add(new EndpointTrace(
                        endpoint,
                        (int)response.StatusCode,
                        HttpSucceeded: false,
                        EncryptedResponseProvided: false,
                        RawHttpResponsePreview: rawPreview,
                        Error: $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"));

                    throw new InvalidOperationException($"Gree Cloud endpoint {endpoint} failed with HTTP {(int)response.StatusCode}.");
                }

                using var responseDocument = JsonDocument.Parse(responseText);
                var encryptedResponse = JsonHelpers.TryGetString(responseDocument.RootElement, "enRes");
                encryptedResponseProvided = !string.IsNullOrWhiteSpace(encryptedResponse);

                endpointTraces.Add(new EndpointTrace(
                    endpoint,
                    (int)response.StatusCode,
                    HttpSucceeded: true,
                    EncryptedResponseProvided: encryptedResponseProvided,
                    RawHttpResponsePreview: rawPreview,
                    Error: null));

                if (string.IsNullOrWhiteSpace(encryptedResponse))
                    throw new InvalidOperationException($"Gree Cloud endpoint {endpoint} response did not contain enRes.");

                return Decrypt(Convert.FromBase64String(encryptedResponse));
            }
            catch (Exception exception) when (exception is not InvalidOperationException)
            {
                endpointTraces.Add(new EndpointTrace(
                    endpoint,
                    HttpStatus: null,
                    HttpSucceeded: false,
                    EncryptedResponseProvided: false,
                    RawHttpResponsePreview: null,
                    Error: exception.GetType().Name));

                throw;
            }
        }

        private static Dictionary<string, object?> PrepareBody(
            IReadOnlyDictionary<string, object?> payload,
            DateTimeOffset now,
            IReadOnlyList<string> hashProps)
        {
            var t = FormatCloudTimestamp(now);
            var r = now.ToUnixTimeSeconds();

            var vc = Md5($"{AppId}_{AppHash}_{t}_{r}");
            var props = hashProps.Select(prop => Convert.ToString(payload[prop], CultureInfo.InvariantCulture) ?? string.Empty);
            var datVc = Md5($"{AppHash}_{string.Join("_", props)}");

            var result = new Dictionary<string, object?>
            {
                ["api"] = new Dictionary<string, object?>
                {
                    ["appId"] = AppId,
                    ["r"] = r,
                    ["t"] = t,
                    ["vc"] = vc
                },
                ["datVc"] = datVc
            };

            foreach (var pair in payload)
                result[pair.Key] = pair.Value;

            return result;
        }

        private static void EnsureLoggedIn()
        {
        }

        private static string FormatCloudTimestamp(DateTimeOffset value) =>
            value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        private static string Md5(string input)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static byte[] Encrypt(string data)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(AesKeyText);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        private static string Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(AesKeyText);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
            return Encoding.UTF8.GetString(decrypted);
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength] + "...<truncated>";
    }


    private static class NormalizeReportCommand
    {
        private const string SnapshotSchemaVersion = "gree-alice-cloud-device-snapshot-v1";

        private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static int Run(string[] args)
        {
            var options = Parse(args);
            Directory.CreateDirectory(options.OutputDirectory);

            var inputReport = ResolveInputReport(options);
            var snapshot = CreateSnapshot(inputReport, options);

            var outputPath = Path.Combine(
                options.OutputDirectory,
                $"gree-cloud-device-snapshot-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json");

            File.WriteAllText(outputPath, JsonSerializer.Serialize(snapshot, SnapshotJsonOptions));

            PrintSnapshotSummary(inputReport, outputPath, snapshot);

            return 0;
        }

        private static NormalizeOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetOption(values, "repo-root") ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var inputReport = GetOption(values, "input-report");
            var outputDir = GetOption(values, "output-dir");

            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "snapshots");

            return new NormalizeOptions(
                repoRoot,
                string.IsNullOrWhiteSpace(inputReport) ? null : Path.GetFullPath(inputReport),
                Path.GetFullPath(outputDir));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--normalize-latest-report", StringComparison.OrdinalIgnoreCase))
                {
                    result["normalize-latest-report"] = null;
                    continue;
                }

                if (!arg.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unexpected argument: {arg}");

                var keyValue = arg[2..];
                var equalsIndex = keyValue.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    result[keyValue[..equalsIndex]] = keyValue[(equalsIndex + 1)..];
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

        private static string? GetOption(IReadOnlyDictionary<string, string?> values, string name)
        {
            return values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
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

        private static string ResolveInputReport(NormalizeOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.InputReportPath))
            {
                if (!File.Exists(options.InputReportPath))
                    throw new FileNotFoundException("Input probe report was not found.", options.InputReportPath);

                return options.InputReportPath;
            }

            var probeDirectory = Path.Combine(options.RepositoryRoot, "artifacts", "gree-alice", "probe");
            if (!Directory.Exists(probeDirectory))
                throw new DirectoryNotFoundException($"Probe artifact directory was not found: {probeDirectory}");

            var latest = Directory
                .GetFiles(probeDirectory, "gree-cloud-probe-*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (latest is null)
                throw new FileNotFoundException($"No Gree Cloud probe reports were found in: {probeDirectory}");

            return latest;
        }

        private static CloudDeviceSnapshotReport CreateSnapshot(string inputReportPath, NormalizeOptions options)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(inputReportPath));
            var root = document.RootElement;
            var generatedAtUtc = DateTimeOffset.UtcNow;

            var devices = new List<CloudDeviceSnapshot>();
            if (TryGetProperty(root, "Devices", out var devicesElement) &&
                devicesElement.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var device in devicesElement.EnumerateArray())
                {
                    devices.Add(CreateDeviceSnapshot(device, index));
                    index++;
                }
            }

            return new CloudDeviceSnapshotReport(
                SchemaVersion: SnapshotSchemaVersion,
                GeneratedAtUtc: generatedAtUtc,
                SourceReportFileName: Path.GetFileName(inputReportPath),
                SourceStage: GetString(root, "Stage"),
                SourceMode: GetString(root, "Mode"),
                SourceTimestampUtc: GetString(root, "TimestampUtc"),
                Region: GetNestedString(root, "Inputs", "Region"),
                ServerUrl: GetNestedString(root, "Inputs", "ServerUrl"),
                HomesCount: GetNestedInt(root, "Summary", "HomesCount"),
                RoomsCount: GetNestedInt(root, "Summary", "RoomsCount"),
                DevicesCount: GetNestedInt(root, "Summary", "DevicesCount"),
                Devices: devices,
                Notes: new[]
                {
                    "Snapshot is generated from a masked probe report.",
                    "Token, password, and raw device key values are intentionally not included.",
                    "Normalized capabilities are candidates and must be confirmed before runtime control is added."
                });
        }

        private static CloudDeviceSnapshot CreateDeviceSnapshot(JsonElement device, int index)
        {
            var rawFieldNames = GetStringArray(device, "RawFieldNames");
            var cloudClassification = GetString(device, "Classification") ?? "unknown";
            var normalizedKind = DetermineNormalizedKind(device, cloudClassification, rawFieldNames);
            var displayName = FirstNonEmpty(
                GetString(device, "DeviceName"),
                GetString(device, "DeviceModel"),
                GetString(device, "DeviceId"),
                $"Gree device {index + 1}");

            var identityConfidence = DetermineIdentityConfidence(device, normalizedKind, rawFieldNames);
            var capabilityDraft = BuildCapabilityDraft(normalizedKind, rawFieldNames);

            return new CloudDeviceSnapshot(
                Index: index,
                DisplayName: displayName,
                HomeName: GetString(device, "HomeName"),
                RoomName: GetString(device, "RoomName"),
                CloudDeviceName: GetString(device, "DeviceName"),
                CloudDeviceId: GetString(device, "DeviceId"),
                CloudDeviceType: GetString(device, "DeviceType"),
                CloudDeviceModel: GetString(device, "DeviceModel"),
                Version: GetString(device, "Version"),
                MacMasked: GetString(device, "Mac"),
                ParentId: GetString(device, "ParentId"),
                ParentMacMasked: GetString(device, "ParentMac"),
                ChildId: GetString(device, "ChildId"),
                ChildMacMasked: GetString(device, "ChildMac"),
                Online: GetNullableBool(device, "Online"),
                KeyProvided: GetBool(device, "KeyProvided"),
                CloudClassification: cloudClassification,
                NormalizedKind: normalizedKind,
                IdentityConfidence: identityConfidence,
                NeedsManualConfirmation: true,
                CapabilityDraft: capabilityDraft,
                SafeRawProperties: GetObjectMap(device, "SafeRawProperties"),
                RawFieldNames: rawFieldNames,
                Notes: BuildDeviceNotes(normalizedKind, cloudClassification, rawFieldNames));
        }

        private static string DetermineNormalizedKind(
            JsonElement device,
            string cloudClassification,
            IReadOnlyList<string> rawFieldNames)
        {
            var haystack = string.Join(
                " ",
                new[]
                {
                    cloudClassification,
                    GetString(device, "DeviceName"),
                    GetString(device, "DeviceType"),
                    GetString(device, "DeviceModel"),
                    GetString(device, "Version"),
                    GetString(device, "ParentId"),
                    GetString(device, "ChildId")
                }.Where(static value => !string.IsNullOrWhiteSpace(value))
                .Concat(rawFieldNames))
                .ToLowerInvariant();

            if (cloudClassification.Equals("vrf-child-unit-candidate", StringComparison.OrdinalIgnoreCase) ||
                haystack.Contains("child", StringComparison.Ordinal) ||
                haystack.Contains("parent", StringComparison.Ordinal))
            {
                return "gree-vrf-child-unit";
            }

            if (cloudClassification.Equals("vrf-gateway-candidate", StringComparison.OrdinalIgnoreCase) ||
                haystack.Contains("vrf", StringComparison.Ordinal) ||
                haystack.Contains("gmv", StringComparison.Ordinal) ||
                haystack.Contains("gateway", StringComparison.Ordinal))
            {
                return "gree-vrf-gateway";
            }

            if (cloudClassification.Equals("split-candidate", StringComparison.OrdinalIgnoreCase) ||
                GetBool(device, "KeyProvided"))
            {
                return "gree-split-or-cloud-ac";
            }

            return "unknown-gree-cloud-device";
        }

        private static string DetermineIdentityConfidence(
            JsonElement device,
            string normalizedKind,
            IReadOnlyList<string> rawFieldNames)
        {
            var hasName = !string.IsNullOrWhiteSpace(GetString(device, "DeviceName"));
            var hasKey = GetBool(device, "KeyProvided");
            var hasVersion = !string.IsNullOrWhiteSpace(GetString(device, "Version"));
            var hasUsefulFields = rawFieldNames.Count >= 5;

            if (normalizedKind.StartsWith("gree-vrf", StringComparison.OrdinalIgnoreCase) &&
                hasName &&
                hasKey &&
                hasVersion &&
                hasUsefulFields)
            {
                return "medium";
            }

            if (hasName && hasKey)
                return "medium-low";

            if (hasName)
                return "low";

            return "unknown";
        }

        private static CloudCapabilityDraft BuildCapabilityDraft(
            string normalizedKind,
            IReadOnlyList<string> rawFieldNames)
        {
            var fieldSet = rawFieldNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return new CloudCapabilityDraft(
                Power: HasAny(fieldSet, "Pow", "power", "pwr", "onOff", "switch") ? "observed-field-candidate" : "not-observed-yet",
                Mode: HasAny(fieldSet, "Mod", "mode", "workMode") ? "observed-field-candidate" : "not-observed-yet",
                TargetTemperature: HasAny(fieldSet, "SetTem", "setTemp", "temperature", "temp") ? "observed-field-candidate" : "not-observed-yet",
                FanSpeed: HasAny(fieldSet, "WdSpd", "fan", "fanSpeed", "windSpeed") ? "observed-field-candidate" : "not-observed-yet",
                Swing: HasAny(fieldSet, "SwUpDn", "SwLfRig", "swing", "swingVertical", "swingHorizontal") ? "observed-field-candidate" : "not-observed-yet",
                CandidateControlTarget: normalizedKind switch
                {
                    "gree-vrf-child-unit" => "room-climate-unit",
                    "gree-vrf-gateway" => "vrf-gateway",
                    "gree-split-or-cloud-ac" => "room-climate-unit",
                    _ => "unknown"
                });
        }

        private static IReadOnlyList<string> BuildDeviceNotes(
            string normalizedKind,
            string cloudClassification,
            IReadOnlyList<string> rawFieldNames)
        {
            var notes = new List<string>
            {
                $"Cloud classification: {cloudClassification}.",
                $"Normalized kind: {normalizedKind}."
            };

            if (normalizedKind == "gree-vrf-child-unit")
                notes.Add("Treat as a room-level climate candidate until parent/gateway relationship is confirmed.");

            if (rawFieldNames.Count > 0)
                notes.Add("Raw field names are kept for future capability mapping; raw values are not copied to this snapshot.");

            return notes;
        }

        private static void PrintSnapshotSummary(
            string inputReportPath,
            string outputPath,
            CloudDeviceSnapshotReport snapshot)
        {
            Console.WriteLine("AssistantEngineer Gree Cloud device snapshot");
            Console.WriteLine("Stage: GREE-ALICE-05");
            Console.WriteLine($"Input report: {inputReportPath}");
            Console.WriteLine($"Output snapshot: {outputPath}");
            Console.WriteLine($"Region: {DisplayValue(snapshot.Region)}");
            Console.WriteLine($"Server URL: {DisplayValue(snapshot.ServerUrl)}");
            Console.WriteLine($"Homes: {snapshot.HomesCount}");
            Console.WriteLine($"Rooms: {snapshot.RoomsCount}");
            Console.WriteLine($"Devices: {snapshot.Devices.Count}");
            Console.WriteLine();

            foreach (var device in snapshot.Devices)
            {
                Console.WriteLine($"Device[{device.Index}]: {device.DisplayName}");
                Console.WriteLine($"  Home/Room: {DisplayValue(device.HomeName)} / {DisplayValue(device.RoomName)}");
                Console.WriteLine($"  Version: {DisplayValue(device.Version)}");
                Console.WriteLine($"  Key provided: {ToYesNo(device.KeyProvided)}");
                Console.WriteLine($"  Cloud classification: {device.CloudClassification}");
                Console.WriteLine($"  Normalized kind: {device.NormalizedKind}");
                Console.WriteLine($"  Identity confidence: {device.IdentityConfidence}");
                Console.WriteLine($"  Candidate control target: {device.CapabilityDraft.CandidateControlTarget}");
                Console.WriteLine();
            }

            Console.WriteLine("Snapshot generated without token/password/raw key values.");
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in element.EnumerateObject())
                {
                    if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        property = item.Value;
                        return true;
                    }
                }
            }

            property = default;
            return false;
        }

        private static string? GetString(JsonElement element, string name)
        {
            if (!TryGetProperty(element, name, out var property))
                return null;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static string? GetNestedString(JsonElement element, string objectName, string propertyName)
        {
            return TryGetProperty(element, objectName, out var nested)
                ? GetString(nested, propertyName)
                : null;
        }

        private static int GetNestedInt(JsonElement element, string objectName, string propertyName)
        {
            if (!TryGetProperty(element, objectName, out var nested) ||
                !TryGetProperty(nested, propertyName, out var property))
            {
                return 0;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
                return number;

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }

            return 0;
        }

        private static bool GetBool(JsonElement element, string name)
        {
            if (!TryGetProperty(element, name, out var property))
                return false;

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(property.GetString(), out var value) => value,
                _ => false
            };
        }

        private static bool? GetNullableBool(JsonElement element, string name)
        {
            if (!TryGetProperty(element, name, out var property))
                return null;

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(property.GetString(), out var value) => value,
                _ => null
            };
        }

        private static IReadOnlyDictionary<string, object?> GetObjectMap(JsonElement element, string name)
        {
            var result = new SortedDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (!TryGetProperty(element, name, out var property) ||
                property.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var item in property.EnumerateObject())
                result[item.Name] = ToSnapshotSafeValue(item.Value);

            return result;
        }

        private static object? ToSnapshotSafeValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.Array => $"<array:{value.GetArrayLength()}>",
                JsonValueKind.Object => "<object>",
                _ => value.GetRawText()
            };
        }
        private static IReadOnlyList<string> GetStringArray(JsonElement element, string name)
        {
            if (!TryGetProperty(element, name, out var property) ||
                property.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return property
                .EnumerateArray()
                .Select(static item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool HasAny(IReadOnlySet<string> fields, params string[] candidates)
        {
            return candidates.Any(fields.Contains);
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? "Gree device";
        }

        private sealed record NormalizeOptions(
            string RepositoryRoot,
            string? InputReportPath,
            string OutputDirectory);

        private sealed record CloudDeviceSnapshotReport(
            string SchemaVersion,
            DateTimeOffset GeneratedAtUtc,
            string SourceReportFileName,
            string? SourceStage,
            string? SourceMode,
            string? SourceTimestampUtc,
            string? Region,
            string? ServerUrl,
            int HomesCount,
            int RoomsCount,
            int DevicesCount,
            IReadOnlyList<CloudDeviceSnapshot> Devices,
            IReadOnlyList<string> Notes);

        private sealed record CloudDeviceSnapshot(
            int Index,
            string DisplayName,
            string? HomeName,
            string? RoomName,
            string? CloudDeviceName,
            string? CloudDeviceId,
            string? CloudDeviceType,
            string? CloudDeviceModel,
            string? Version,
            string? MacMasked,
            string? ParentId,
            string? ParentMacMasked,
            string? ChildId,
            string? ChildMacMasked,
            bool? Online,
            bool KeyProvided,
            string CloudClassification,
            string NormalizedKind,
            string IdentityConfidence,
            bool NeedsManualConfirmation,
            CloudCapabilityDraft CapabilityDraft,
            IReadOnlyDictionary<string, object?> SafeRawProperties,
            IReadOnlyList<string> RawFieldNames,
            IReadOnlyList<string> Notes);

        private sealed record CloudCapabilityDraft(
            string Power,
            string Mode,
            string TargetTemperature,
            string FanSpeed,
            string Swing,
            string CandidateControlTarget);
    }
    private static class JsonHelpers
    {
        public static string? TryGetString(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in element.EnumerateObject())
            {
                if (!names.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                return ConvertToString(property.Value);
            }

            return null;
        }

        public static string? TryGetStringRecursive(JsonElement element, params string[] names)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var direct = TryGetString(element, names);
                if (!string.IsNullOrWhiteSpace(direct))
                    return direct;

                foreach (var property in element.EnumerateObject())
                {
                    var nested = TryGetStringRecursive(property.Value, names);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = TryGetStringRecursive(item, names);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }

            return null;
        }

        public static bool TryGetArray(JsonElement element, out JsonElement array, params string[] names)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (names.Contains(property.Name, StringComparer.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.Array)
                    {
                        array = property.Value;
                        return true;
                    }
                }
            }

            array = default;
            return false;
        }

        public static bool TryGetArrayRecursive(JsonElement element, out JsonElement array, params string[] names)
        {
            if (TryGetArray(element, out array, names))
                return true;

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (TryGetArrayRecursive(property.Value, out array, names))
                        return true;
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (TryGetArrayRecursive(item, out array, names))
                        return true;
                }
            }

            array = default;
            return false;
        }

        public static bool? TryGetBool(JsonElement element, params string[] names)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in element.EnumerateObject())
            {
                if (!names.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (property.Value.ValueKind == JsonValueKind.True)
                    return true;

                if (property.Value.ValueKind == JsonValueKind.False)
                    return false;

                var value = ConvertToString(property.Value);
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("online", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("offline", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return null;
        }

        public static IReadOnlyList<string> GetPropertyNames(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return Array.Empty<string>();

            return element
                .EnumerateObject()
                .Select(static property => property.Name)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }


        public static IReadOnlyDictionary<string, object?> CreateSafePropertyMap(JsonElement element, bool maskSecrets)
        {
            var result = new SortedDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (element.ValueKind != JsonValueKind.Object)
                return result;

            foreach (var property in element.EnumerateObject())
            {
                if (maskSecrets && IsSensitiveName(property.Name))
                {
                    result[property.Name] = "<masked>";
                    continue;
                }

                result[property.Name] = ConvertToSafeRawValue(property.Value);
            }

            return result;
        }

        private static object? ConvertToSafeRawValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.Array => $"<array:{value.GetArrayLength()}>",
                JsonValueKind.Object => "<object>",
                _ => value.GetRawText()
            };
        }
        public static string GetRootFieldNames(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return $"<{element.ValueKind}>";

            var names = element
                .EnumerateObject()
                .Select(static property => property.Name)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return names.Length == 0
                ? "<empty object>"
                : string.Join(", ", names);
        }

        public static string CreateSafePreview(string json, bool maskSecrets, int maxLength)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                {
                    WriteMaskedElement(writer, document.RootElement, maskSecrets);
                }

                var masked = Encoding.UTF8.GetString(stream.ToArray());
                return masked.Length <= maxLength ? masked : masked[..maxLength] + "...<truncated>";
            }
            catch
            {
                return json.Length <= maxLength ? json : json[..maxLength] + "...<truncated>";
            }
        }

        private static void WriteMaskedElement(Utf8JsonWriter writer, JsonElement element, bool maskSecrets)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        if (maskSecrets && IsSensitiveName(property.Name))
                            writer.WriteStringValue("<masked>");
                        else
                            WriteMaskedElement(writer, property.Value, maskSecrets);
                    }

                    writer.WriteEndObject();
                    return;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                        WriteMaskedElement(writer, item, maskSecrets);

                    writer.WriteEndArray();
                    return;

                case JsonValueKind.String:
                    writer.WriteStringValue(element.GetString());
                    return;

                case JsonValueKind.Number:
                    writer.WriteRawValue(element.GetRawText());
                    return;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    return;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    return;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    writer.WriteNullValue();
                    return;

                default:
                    writer.WriteStringValue(element.GetRawText());
                    return;
            }
        }

        private static bool IsSensitiveName(string name)
        {
            return name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("psw", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("key", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Key", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("mail", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("email", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("mac", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("barcode", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("user", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("username", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ConvertToString(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => value.GetRawText()
            };
        }
    }
}
