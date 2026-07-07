using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class LiveStatusProbeCommand
{
    private const string StageName = "GREE-ALICE-06";
    private const string ModeName = "live-status-probe";

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

    public static async Task<int> RunAsync(string[] args)
    {
        var options = LiveStatusOptions.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        LiveStatusProbeReport report;

        if (options.ConfigurationOnly || !options.HasCredentials)
        {
            report = LiveStatusProbeReport.ConfigurationOnly(options, timestampUtc);
        }
        else
        {
            report = await RunLiveProbeAsync(options, timestampUtc);
        }

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-cloud-live-status-probe-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, ReportJsonOptions));
        PrintSummary(options, report, outputPath);

        return report.Errors.Count == 0 ? 0 : 2;
    }

    private static async Task<LiveStatusProbeReport> RunLiveProbeAsync(
        LiveStatusOptions options,
        DateTimeOffset timestampUtc)
    {
        var client = new GreeCloudLiveStatusClient(options);

        try
        {
            var result = await client.ProbeAsync();

            return new LiveStatusProbeReport(
                Stage: StageName,
                Mode: ModeName,
                TimestampUtc: timestampUtc,
                Inputs: LiveStatusInputs.FromOptions(options),
                Auth: new LiveStatusAuth(
                    LoginAttempted: true,
                    LoginSucceeded: true,
                    UserId: result.UserId,
                    TokenProvided: !string.IsNullOrWhiteSpace(result.Token),
                    TokenMasked: MaskLongValue(result.Token, options.MaskSecrets)),
                Summary: LiveStatusSummary.From(result.Devices, result.EndpointAttempts),
                Devices: result.Devices,
                EndpointAttempts: result.EndpointAttempts,
                Errors: Array.Empty<LiveStatusError>(),
                Notes: new[]
                {
                    "Only read-only candidate endpoints were probed.",
                    "No runtime control commands were sent.",
                    "Sensitive values are masked by default in report artifacts."
                });
        }
        catch (Exception exception)
        {
            return new LiveStatusProbeReport(
                Stage: StageName,
                Mode: ModeName,
                TimestampUtc: timestampUtc,
                Inputs: LiveStatusInputs.FromOptions(options),
                Auth: new LiveStatusAuth(
                    LoginAttempted: true,
                    LoginSucceeded: false,
                    UserId: null,
                    TokenProvided: false,
                    TokenMasked: null),
                Summary: LiveStatusSummary.Empty,
                Devices: Array.Empty<LiveStatusDevice>(),
                EndpointAttempts: client.EndpointAttempts,
                Errors: new[]
                {
                    new LiveStatusError("GREE_CLOUD_LIVE_STATUS_PROBE_FAILED", SanitizedError(exception, options))
                },
                Notes: new[]
                {
                    "Live status probe failed before completion.",
                    "Check credentials, server URL, network access, and the masked endpoint attempts."
                });
        }
    }

    private static void PrintSummary(
        LiveStatusOptions options,
        LiveStatusProbeReport report,
        string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree Cloud live status probe");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Region: {DisplayValue(options.Region)}");
        Console.WriteLine($"Server URL: {DisplayValue(options.ServerUrl)}");
        Console.WriteLine($"Username: {MaskValue(options.Username, options.MaskSecrets)}");
        Console.WriteLine($"Password provided: {ToYesNo(!string.IsNullOrWhiteSpace(options.Password))}");
        Console.WriteLine($"Timeout seconds: {options.TimeoutSeconds}");
        Console.WriteLine($"Max attempts: {options.MaxAttempts}");
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
        Console.WriteLine($"  Devices: {report.Summary.DevicesCount}");
        Console.WriteLine($"  Endpoint attempts: {report.Summary.EndpointAttemptsCount}");
        Console.WriteLine($"  HTTP succeeded: {report.Summary.HttpSucceededCount}");
        Console.WriteLine($"  Decryption succeeded: {report.Summary.DecryptionSucceededCount}");
        Console.WriteLine($"  Attempts with candidate fields: {report.Summary.AttemptsWithCandidateFieldsCount}");
        Console.WriteLine($"  Unique candidate fields: {report.Summary.UniqueCandidateFieldsCount}");

        if (report.Devices.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Devices:");
            foreach (var device in report.Devices)
            {
                Console.WriteLine($"  - {DisplayValue(device.DeviceName)}");
                Console.WriteLine($"    Version: {DisplayValue(device.Version)}");
                Console.WriteLine($"    Classification: {device.Classification}");
                Console.WriteLine($"    Online field: {DisplayValue(device.Online?.ToString())}");
                Console.WriteLine($"    Key provided: {ToYesNo(device.KeyProvided)}");
            }
        }

        var attemptsWithFields = report.EndpointAttempts
            .Where(static attempt => attempt.CandidateFieldNames.Count > 0)
            .Take(10)
            .ToArray();

        if (attemptsWithFields.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Candidate fields found:");
            foreach (var attempt in attemptsWithFields)
            {
                Console.WriteLine($"  - {attempt.Endpoint} / {attempt.PayloadShape} / {attempt.HashShape}");
                Console.WriteLine($"    Fields: {string.Join(", ", attempt.CandidateFieldNames)}");
            }
        }
        else if (report.Mode != "configuration-only")
        {
            Console.WriteLine();
            Console.WriteLine("No live capability fields were found in the attempted read-only endpoints yet.");
            Console.WriteLine("Inspect the masked JSON report for endpoint responses and cloud messages.");
        }

        if (report.Errors.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Errors:");
            Console.ResetColor();

            foreach (var error in report.Errors)
                Console.WriteLine($"  - {error.Code}: {error.Message}");
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

    private static string SanitizedError(Exception exception, LiveStatusOptions options)
    {
        var message = exception.Message;

        if (!string.IsNullOrWhiteSpace(options.Username))
            message = message.Replace(options.Username, MaskValue(options.Username, maskSecrets: true), StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(options.Password))
            message = message.Replace(options.Password, "<password>", StringComparison.OrdinalIgnoreCase);

        return message;
    }

    private sealed record LiveStatusOptions(
        string RepositoryRoot,
        string Region,
        string ServerUrl,
        string Username,
        string Password,
        string OutputDirectory,
        int TimeoutSeconds,
        int MaxAttempts,
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

                ["middleeast"] = "https://megrih.gree.com",
                ["middleeastserver"] = "https://megrih.gree.com",
                ["me"] = "https://megrih.gree.com",
                ["megrih"] = "https://megrih.gree.com",

                ["ouzbekistan"] = "https://hkgrih.gree.com",
                ["ouzbékistan"] = "https://hkgrih.gree.com",
                ["uzbekistan"] = "https://hkgrih.gree.com",
                ["узбекистан"] = "https://hkgrih.gree.com",

                ["russianserver"] = "https://rugrih.gree.com",
                ["russia"] = "https://rugrih.gree.com",
                ["ru"] = "https://rugrih.gree.com",
                ["rugrih"] = "https://rugrih.gree.com"
            };

        public bool HasCredentials =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        public static LiveStatusOptions Parse(string[] args)
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
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "live-status");

            outputDir = Path.GetFullPath(outputDir);

            var timeoutRaw = GetValue(values, "timeout-seconds", "GREE_ALICE_TIMEOUT_SECONDS");
            var timeoutSeconds = ParseInt(timeoutRaw, 15, 1, 300, "timeout-seconds");

            var maxAttemptsRaw = GetValue(values, "max-attempts", "GREE_ALICE_MAX_LIVE_STATUS_ATTEMPTS");
            var maxAttempts = ParseInt(maxAttemptsRaw, 40, 1, 200, "max-attempts");

            var saveRawResponse = ParseBool(
                GetValue(values, "save-raw-response", "GREE_ALICE_SAVE_RAW_RESPONSE"),
                values.ContainsKey("save-raw-response"));

            var maskSecrets = values.ContainsKey("no-mask-secrets")
                ? false
                : ParseBool(GetValue(values, "mask-secrets", "GREE_ALICE_MASK_SECRETS"), defaultValue: true);

            var configurationOnly = values.ContainsKey("configuration-only");

            return new LiveStatusOptions(
                repoRoot,
                region.Trim(),
                NormalizeServerUrl(serverUrl),
                username.Trim(),
                password,
                outputDir,
                timeoutSeconds,
                maxAttempts,
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

                if (arg.Equals("--probe-live-status", StringComparison.OrdinalIgnoreCase))
                {
                    result["probe-live-status"] = null;
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

    private sealed record LiveStatusProbeReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        LiveStatusInputs Inputs,
        LiveStatusAuth Auth,
        LiveStatusSummary Summary,
        IReadOnlyList<LiveStatusDevice> Devices,
        IReadOnlyList<LiveStatusEndpointAttempt> EndpointAttempts,
        IReadOnlyList<LiveStatusError> Errors,
        IReadOnlyList<string> Notes)
    {
        public static LiveStatusProbeReport ConfigurationOnly(
            LiveStatusOptions options,
            DateTimeOffset timestampUtc)
        {
            return new LiveStatusProbeReport(
                StageName,
                "configuration-only",
                timestampUtc,
                LiveStatusInputs.FromOptions(options),
                new LiveStatusAuth(LoginAttempted: false, LoginSucceeded: false, UserId: null, TokenProvided: false, TokenMasked: null),
                LiveStatusSummary.Empty,
                Array.Empty<LiveStatusDevice>(),
                Array.Empty<LiveStatusEndpointAttempt>(),
                Array.Empty<LiveStatusError>(),
                new[]
                {
                    "Live status endpoints were not probed.",
                    "Set credentials and run with --probe-live-status without --configuration-only."
                });
        }
    }

    private sealed record LiveStatusInputs(
        string Region,
        string ServerUrl,
        string Username,
        bool PasswordProvided,
        int TimeoutSeconds,
        int MaxAttempts,
        bool SaveRawResponse,
        bool MaskSecrets)
    {
        public static LiveStatusInputs FromOptions(LiveStatusOptions options)
        {
            return new LiveStatusInputs(
                options.Region,
                options.ServerUrl,
                MaskValue(options.Username, options.MaskSecrets),
                !string.IsNullOrWhiteSpace(options.Password),
                options.TimeoutSeconds,
                options.MaxAttempts,
                options.SaveRawResponse,
                options.MaskSecrets);
        }
    }

    private sealed record LiveStatusAuth(
        bool LoginAttempted,
        bool LoginSucceeded,
        string? UserId,
        bool TokenProvided,
        string? TokenMasked);

    private sealed record LiveStatusSummary(
        int DevicesCount,
        int EndpointAttemptsCount,
        int HttpSucceededCount,
        int EncryptedResponseProvidedCount,
        int DecryptionSucceededCount,
        int AttemptsWithCandidateFieldsCount,
        int UniqueCandidateFieldsCount)
    {
        public static LiveStatusSummary Empty { get; } = new(
            DevicesCount: 0,
            EndpointAttemptsCount: 0,
            HttpSucceededCount: 0,
            EncryptedResponseProvidedCount: 0,
            DecryptionSucceededCount: 0,
            AttemptsWithCandidateFieldsCount: 0,
            UniqueCandidateFieldsCount: 0);

        public static LiveStatusSummary From(
            IReadOnlyList<LiveStatusDevice> devices,
            IReadOnlyList<LiveStatusEndpointAttempt> attempts)
        {
            var candidateFields = attempts
                .SelectMany(static attempt => attempt.CandidateFieldNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return new LiveStatusSummary(
                DevicesCount: devices.Count,
                EndpointAttemptsCount: attempts.Count,
                HttpSucceededCount: attempts.Count(static attempt => attempt.HttpSucceeded),
                EncryptedResponseProvidedCount: attempts.Count(static attempt => attempt.EncryptedResponseProvided),
                DecryptionSucceededCount: attempts.Count(static attempt => attempt.DecryptionSucceeded),
                AttemptsWithCandidateFieldsCount: attempts.Count(static attempt => attempt.CandidateFieldNames.Count > 0),
                UniqueCandidateFieldsCount: candidateFields);
        }
    }

    private sealed record LiveStatusDevice(
        string? HomeId,
        string? HomeName,
        string? RoomId,
        string? RoomName,
        string? DeviceId,
        string? DeviceName,
        string? DeviceType,
        string? DeviceModel,
        string? Version,
        string? MacMasked,
        bool? Online,
        bool KeyProvided,
        string Classification,
        IReadOnlyList<string> RawFieldNames);

    private sealed record LiveStatusEndpointAttempt(
        string DeviceName,
        string Endpoint,
        string PayloadShape,
        string HashShape,
        int? HttpStatus,
        bool HttpSucceeded,
        bool EncryptedResponseProvided,
        bool DecryptionSucceeded,
        string? CloudCode,
        string? CloudMessage,
        IReadOnlyList<string> RootFieldNames,
        IReadOnlyList<string> CandidateFieldNames,
        string? SanitizedResponsePreview,
        string? RawHttpResponsePreview,
        string? Error);

    private sealed record LiveStatusError(
        string Code,
        string Message);

    private sealed record LiveStatusProbeResult(
        string? UserId,
        string? Token,
        IReadOnlyList<LiveStatusDevice> Devices,
        IReadOnlyList<LiveStatusEndpointAttempt> EndpointAttempts);

    private sealed record HomeContext(
        string? HomeId,
        string? HomeName);

    private sealed record DeviceContext(
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
        string? Key,
        string? Hid,
        string? Mid,
        bool? Online,
        IReadOnlyList<string> RawFieldNames)
    {
        public LiveStatusDevice ToReportDevice(bool maskSecrets)
        {
            return new LiveStatusDevice(
                HomeId,
                HomeName,
                RoomId,
                RoomName,
                DeviceId,
                DeviceName,
                DeviceType,
                DeviceModel,
                Version,
                MaskLongValue(Mac, maskSecrets),
                Online,
                KeyProvided: !string.IsNullOrWhiteSpace(Key),
                Classification: Classify(),
                RawFieldNames);
        }

        private string Classify()
        {
            var haystack = string.Join(
                " ",
                new[]
                {
                    DeviceName,
                    DeviceType,
                    DeviceModel,
                    Version,
                    Hid,
                    Mid
                }.Where(static value => !string.IsNullOrWhiteSpace(value))
                .Concat(RawFieldNames))
                .ToLowerInvariant();

            if (haystack.Contains("vrf", StringComparison.Ordinal) ||
                haystack.Contains("gmv", StringComparison.Ordinal) ||
                haystack.Contains("gateway", StringComparison.Ordinal) ||
                haystack.Contains("hid", StringComparison.Ordinal))
            {
                return "vrf-or-gateway-candidate";
            }

            if (!string.IsNullOrWhiteSpace(Key) ||
                haystack.Contains("split", StringComparison.Ordinal) ||
                haystack.Contains("air", StringComparison.Ordinal) ||
                haystack.Contains("conditioner", StringComparison.Ordinal))
            {
                return "room-climate-candidate";
            }

            return "unknown";
        }
    }

    private sealed record EndpointCandidate(string Endpoint);

    private sealed record PayloadCandidate(
        string Name,
        IReadOnlyDictionary<string, object?> Payload,
        IReadOnlyList<string> HashProps);

    private sealed class GreeCloudLiveStatusClient
    {
        private const string AppId = "4920681951525131286";
        private const string AppHash = "0fa513124aa97781d1f3f40d61ca1a89";
        private const string AesKeyText = "#G$&^jgfujy6ujxt";
        private const string Gaen1Header = "5ac2bdf935bcca70";

        private static readonly EndpointCandidate[] EndpointCandidates =
        {
            new("/App/GetDeviceStatus"),
            new("/App/GetDevStatus"),
            new("/App/GetDevState"),
            new("/App/GetDevInfo"),
            new("/App/GetDevAttrs"),
            new("/App/GetDeviceAttrs"),
            new("/App/GetDevParams"),
            new("/App/GetDeviceParams")
        };

        private readonly LiveStatusOptions options;
        private readonly HttpClient httpClient;
        private readonly List<LiveStatusEndpointAttempt> endpointAttempts = new();

        private string? userId;
        private string? token;

        public GreeCloudLiveStatusClient(LiveStatusOptions options)
        {
            this.options = options;
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.ServerUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        }

        public IReadOnlyList<LiveStatusEndpointAttempt> EndpointAttempts => endpointAttempts;

        public async Task<LiveStatusProbeResult> ProbeAsync()
        {
            await LoginAsync();

            var homes = await GetHomesAsync();
            var deviceContexts = new List<DeviceContext>();
            foreach (var home in homes)
            {
                var devices = await GetDevicesAsync(home);
                deviceContexts.AddRange(devices);
            }

            var reportedDevices = deviceContexts
                .Select(device => device.ToReportDevice(options.MaskSecrets))
                .ToArray();

            var attemptsCount = 0;
            foreach (var device in deviceContexts)
            {
                foreach (var endpoint in EndpointCandidates)
                {
                    foreach (var payload in BuildPayloadCandidates(device))
                    {
                        if (attemptsCount >= options.MaxAttempts)
                            return new LiveStatusProbeResult(userId, token, reportedDevices, endpointAttempts);

                        attemptsCount++;
                        var attempt = await TryEndpointAsync(device, endpoint, payload);
                        endpointAttempts.Add(attempt);
                    }
                }
            }

            return new LiveStatusProbeResult(userId, token, reportedDevices, endpointAttempts);
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

        private async Task<IReadOnlyList<HomeContext>> GetHomesAsync()
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
                return Array.Empty<HomeContext>();

            var homes = new List<HomeContext>();
            foreach (var home in homesArray.EnumerateArray())
            {
                homes.Add(new HomeContext(
                    JsonHelpers.TryGetString(home, "id", "homeId", "hid"),
                    JsonHelpers.TryGetString(home, "name", "homeName")));
            }

            return homes;
        }

        private async Task<IReadOnlyList<DeviceContext>> GetDevicesAsync(HomeContext home)
        {
            EnsureLoggedIn();

            if (string.IsNullOrWhiteSpace(home.HomeId))
                return Array.Empty<DeviceContext>();

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
            var devices = new List<DeviceContext>();

            if (JsonHelpers.TryGetArrayRecursive(document.RootElement, out var roomsArray, "rooms", "room"))
            {
                foreach (var room in roomsArray.EnumerateArray())
                {
                    if (!JsonHelpers.TryGetArray(room, out var roomDevices, "devs", "devices"))
                        continue;

                    foreach (var device in roomDevices.EnumerateArray())
                        devices.Add(CreateDeviceContext(home, room, device));
                }

                return devices;
            }

            if (JsonHelpers.TryGetArrayRecursive(document.RootElement, out var devicesArray, "devs", "devices"))
            {
                foreach (var device in devicesArray.EnumerateArray())
                    devices.Add(CreateDeviceContext(home, default, device));
            }

            return devices;
        }

        private static DeviceContext CreateDeviceContext(HomeContext home, JsonElement room, JsonElement device)
        {
            var roomId = room.ValueKind == JsonValueKind.Object
                ? JsonHelpers.TryGetString(room, "id", "roomId", "rid")
                : null;
            var roomName = room.ValueKind == JsonValueKind.Object
                ? JsonHelpers.TryGetString(room, "name", "roomName")
                : null;

            return new DeviceContext(
                HomeId: home.HomeId,
                HomeName: home.HomeName,
                RoomId: roomId,
                RoomName: roomName,
                DeviceId: JsonHelpers.TryGetString(device, "id", "devId", "deviceId", "did"),
                DeviceName: JsonHelpers.TryGetString(device, "name", "devName", "deviceName"),
                DeviceType: JsonHelpers.TryGetString(device, "type", "devType", "deviceType", "category"),
                DeviceModel: JsonHelpers.TryGetString(device, "model", "devModel", "prodModel"),
                Version: JsonHelpers.TryGetString(device, "ver", "version"),
                Mac: JsonHelpers.TryGetString(device, "mac", "deviceMac"),
                Key: JsonHelpers.TryGetString(device, "key", "aesKey", "secretKey"),
                Hid: JsonHelpers.TryGetString(device, "hid"),
                Mid: JsonHelpers.TryGetString(device, "mid"),
                Online: JsonHelpers.TryGetBool(device, "online", "isOnline", "status"),
                RawFieldNames: JsonHelpers.GetPropertyNames(device));
        }

        private IEnumerable<PayloadCandidate> BuildPayloadCandidates(DeviceContext device)
        {
            EnsureLoggedIn();

            if (!string.IsNullOrWhiteSpace(device.Mac))
            {
                yield return new PayloadCandidate(
                    "token_uid_mac",
                    BuildPayload(("token", token), ("uid", userId), ("mac", device.Mac)),
                    new[] { "token", "uid", "mac" });

                if (!string.IsNullOrWhiteSpace(device.Key))
                {
                    yield return new PayloadCandidate(
                        "token_uid_mac_key",
                        BuildPayload(("token", token), ("uid", userId), ("mac", device.Mac), ("key", device.Key)),
                        new[] { "token", "uid", "mac", "key" });
                }
            }

            if (!string.IsNullOrWhiteSpace(device.DeviceId))
            {
                yield return new PayloadCandidate(
                    "token_uid_devId",
                    BuildPayload(("token", token), ("uid", userId), ("devId", device.DeviceId)),
                    new[] { "token", "uid", "devId" });
            }

            if (!string.IsNullOrWhiteSpace(device.HomeId) &&
                !string.IsNullOrWhiteSpace(device.Mac))
            {
                var homeIdPayload = long.TryParse(device.HomeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHomeId)
                    ? parsedHomeId
                    : (object)device.HomeId;

                yield return new PayloadCandidate(
                    "token_uid_homeId_mac",
                    BuildPayload(("token", token), ("uid", userId), ("homeId", homeIdPayload), ("mac", device.Mac)),
                    new[] { "token", "uid", "homeId", "mac" });
            }

            if (!string.IsNullOrWhiteSpace(device.Hid) &&
                !string.IsNullOrWhiteSpace(device.Mac))
            {
                yield return new PayloadCandidate(
                    "token_uid_hid_mac",
                    BuildPayload(("token", token), ("uid", userId), ("hid", device.Hid), ("mac", device.Mac)),
                    new[] { "token", "uid", "hid", "mac" });
            }
        }

        private static Dictionary<string, object?> BuildPayload(params (string Key, object? Value)[] values)
        {
            var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var value in values)
                payload[value.Key] = value.Value;

            return payload;
        }

        private async Task<LiveStatusEndpointAttempt> TryEndpointAsync(
            DeviceContext device,
            EndpointCandidate endpoint,
            PayloadCandidate payloadCandidate)
        {
            var now = DateTimeOffset.UtcNow;
            var rawHttpPreview = default(string);
            var decryptedPreview = default(string);
            IReadOnlyList<string> rootFields = Array.Empty<string>();
            IReadOnlyList<string> candidateFields = Array.Empty<string>();
            var cloudCode = default(string);
            var cloudMessage = default(string);
            var httpStatus = default(int?);
            var httpSucceeded = false;
            var encryptedResponseProvided = false;
            var decryptionSucceeded = false;

            try
            {
                var requestBody = PrepareBody(payloadCandidate.Payload, now, payloadCandidate.HashProps);
                var jsonBody = JsonSerializer.Serialize(requestBody, CompactJsonOptions);
                var encryptedBody = Convert.ToBase64String(Encrypt(jsonBody));

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Endpoint)
                {
                    Content = new StringContent(encryptedBody, Encoding.UTF8)
                };

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Headers.TryAddWithoutValidation("Gaen1", Gaen1Header);
                request.Headers.TryAddWithoutValidation("Charset", "utf-8");

                using var response = await httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                httpStatus = (int)response.StatusCode;
                httpSucceeded = response.IsSuccessStatusCode;

                if (options.SaveRawResponse)
                    rawHttpPreview = Truncate(responseText, 2000);

                if (!response.IsSuccessStatusCode)
                {
                    return new LiveStatusEndpointAttempt(
                        DeviceName: device.DeviceName ?? "<unnamed>",
                        Endpoint: endpoint.Endpoint,
                        PayloadShape: payloadCandidate.Name,
                        HashShape: string.Join("_", payloadCandidate.HashProps),
                        HttpStatus: httpStatus,
                        HttpSucceeded: false,
                        EncryptedResponseProvided: false,
                        DecryptionSucceeded: false,
                        CloudCode: null,
                        CloudMessage: null,
                        RootFieldNames: rootFields,
                        CandidateFieldNames: candidateFields,
                        SanitizedResponsePreview: null,
                        RawHttpResponsePreview: rawHttpPreview,
                        Error: $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                using var responseDocument = JsonDocument.Parse(responseText);
                var encryptedResponse = JsonHelpers.TryGetString(responseDocument.RootElement, "enRes");
                encryptedResponseProvided = !string.IsNullOrWhiteSpace(encryptedResponse);

                if (string.IsNullOrWhiteSpace(encryptedResponse))
                {
                    return new LiveStatusEndpointAttempt(
                        DeviceName: device.DeviceName ?? "<unnamed>",
                        Endpoint: endpoint.Endpoint,
                        PayloadShape: payloadCandidate.Name,
                        HashShape: string.Join("_", payloadCandidate.HashProps),
                        HttpStatus: httpStatus,
                        HttpSucceeded: httpSucceeded,
                        EncryptedResponseProvided: false,
                        DecryptionSucceeded: false,
                        CloudCode: JsonHelpers.TryGetStringRecursive(responseDocument.RootElement, "r", "code"),
                        CloudMessage: JsonHelpers.TryGetStringRecursive(responseDocument.RootElement, "msg", "message"),
                        RootFieldNames: JsonHelpers.GetRootFieldNamesArray(responseDocument.RootElement),
                        CandidateFieldNames: candidateFields,
                        SanitizedResponsePreview: JsonHelpers.CreateSafePreview(responseText, options.MaskSecrets, maxLength: 1500),
                        RawHttpResponsePreview: rawHttpPreview,
                        Error: "Response did not contain enRes.");
                }

                var decrypted = Decrypt(Convert.FromBase64String(encryptedResponse));
                decryptionSucceeded = true;
                decryptedPreview = JsonHelpers.CreateSafePreview(decrypted, options.MaskSecrets, maxLength: 2500);

                using var decryptedDocument = JsonDocument.Parse(decrypted);
                rootFields = JsonHelpers.GetRootFieldNamesArray(decryptedDocument.RootElement);
                candidateFields = JsonHelpers.FindCandidateFieldNames(decryptedDocument.RootElement);
                cloudCode = JsonHelpers.TryGetStringRecursive(decryptedDocument.RootElement, "r", "code");
                cloudMessage = JsonHelpers.TryGetStringRecursive(decryptedDocument.RootElement, "msg", "message");

                return new LiveStatusEndpointAttempt(
                    DeviceName: device.DeviceName ?? "<unnamed>",
                    Endpoint: endpoint.Endpoint,
                    PayloadShape: payloadCandidate.Name,
                    HashShape: string.Join("_", payloadCandidate.HashProps),
                    HttpStatus: httpStatus,
                    HttpSucceeded: httpSucceeded,
                    EncryptedResponseProvided: encryptedResponseProvided,
                    DecryptionSucceeded: decryptionSucceeded,
                    CloudCode: cloudCode,
                    CloudMessage: cloudMessage,
                    RootFieldNames: rootFields,
                    CandidateFieldNames: candidateFields,
                    SanitizedResponsePreview: decryptedPreview,
                    RawHttpResponsePreview: rawHttpPreview,
                    Error: null);
            }
            catch (Exception exception)
            {
                return new LiveStatusEndpointAttempt(
                    DeviceName: device.DeviceName ?? "<unnamed>",
                    Endpoint: endpoint.Endpoint,
                    PayloadShape: payloadCandidate.Name,
                    HashShape: string.Join("_", payloadCandidate.HashProps),
                    HttpStatus: httpStatus,
                    HttpSucceeded: httpSucceeded,
                    EncryptedResponseProvided: encryptedResponseProvided,
                    DecryptionSucceeded: decryptionSucceeded,
                    CloudCode: cloudCode,
                    CloudMessage: cloudMessage,
                    RootFieldNames: rootFields,
                    CandidateFieldNames: candidateFields,
                    SanitizedResponsePreview: decryptedPreview,
                    RawHttpResponsePreview: rawHttpPreview,
                    Error: exception.GetType().Name);
            }
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

            using var response = await httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gree Cloud endpoint {endpoint} failed with HTTP {(int)response.StatusCode}.");

            using var responseDocument = JsonDocument.Parse(responseText);
            var encryptedResponse = JsonHelpers.TryGetString(responseDocument.RootElement, "enRes");

            if (string.IsNullOrWhiteSpace(encryptedResponse))
                throw new InvalidOperationException($"Gree Cloud endpoint {endpoint} response did not contain enRes.");

            return Decrypt(Convert.FromBase64String(encryptedResponse));
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

        private void EnsureLoggedIn()
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Cloud client is not logged in.");
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

    private static class JsonHelpers
    {
        private static readonly string[] CandidateFieldNames =
        {
            "Pow",
            "power",
            "pwr",
            "onOff",
            "switch",
            "Mod",
            "mode",
            "workMode",
            "SetTem",
            "setTemp",
            "targetTemp",
            "temperature",
            "temp",
            "TemSen",
            "sensorTemp",
            "WdSpd",
            "fan",
            "fanSpeed",
            "windSpeed",
            "SwUpDn",
            "SwLfRig",
            "swing",
            "swingVertical",
            "swingHorizontal",
            "ErrCode",
            "errorCode",
            "health",
            "status"
        };

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

        public static string GetRootFieldNames(JsonElement element)
        {
            return string.Join(", ", GetRootFieldNamesArray(element));
        }

        public static IReadOnlyList<string> GetRootFieldNamesArray(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return new[] { $"<{element.ValueKind}>" };

            var names = element
                .EnumerateObject()
                .Select(static property => property.Name)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return names.Length == 0
                ? new[] { "<empty object>" }
                : names;
        }

        public static IReadOnlyList<string> FindCandidateFieldNames(JsonElement element)
        {
            var found = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            FindCandidateFieldNames(element, found);
            return found.ToArray();
        }

        private static void FindCandidateFieldNames(JsonElement element, ISet<string> found)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (CandidateFieldNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                        found.Add(property.Name);

                    FindCandidateFieldNames(property.Value, found);
                }

                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    FindCandidateFieldNames(item, found);
            }
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
