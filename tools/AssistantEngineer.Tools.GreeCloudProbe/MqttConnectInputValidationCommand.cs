using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttConnectInputValidationCommand
{
    private const string StageName = "GREE-ALICE-12";
    private const string ModeName = "mqtt-connect-input-validation";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Run(string[] args)
    {
        var options = Options.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? Report.ConfigurationOnly(options, timestampUtc)
            : BuildReport(options, timestampUtc);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-connect-input-validation-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(report, outputPath);

        return 0;
    }

    private static Report BuildReport(Options options, DateTimeOffset timestampUtc)
    {
        var specs = BuildSpecs();
        var observations = specs.Select(Observe).ToArray();
        var violations = new List<string>();

        foreach (var observation in observations)
        {
            if (observation.RequiredForFutureConnectOnlyTest && !observation.Provided)
                violations.Add($"{observation.Name} is required for a future CONNECT-only test but is missing.");

            if (!observation.Valid)
                violations.Add($"{observation.Name} is invalid: {observation.ValidationMessage}");
        }

        var authMode = GetRaw("GREE_ALICE_MQTT_AUTH_MODE");
        var authModeNormalized = string.IsNullOrWhiteSpace(authMode)
            ? null
            : authMode.Trim().ToLowerInvariant();

        var passwordProvided = !string.IsNullOrWhiteSpace(GetRaw("GREE_ALICE_MQTT_PASSWORD"));
        var tokenProvided = !string.IsNullOrWhiteSpace(GetRaw("GREE_ALICE_MQTT_TOKEN"));

        if (authModeNormalized == "password")
        {
            if (!passwordProvided)
                violations.Add("GREE_ALICE_MQTT_AUTH_MODE=password requires GREE_ALICE_MQTT_PASSWORD.");
            if (tokenProvided)
                violations.Add("GREE_ALICE_MQTT_AUTH_MODE=password must not include GREE_ALICE_MQTT_TOKEN in the first safe validation scaffold.");
        }
        else if (authModeNormalized == "token")
        {
            if (!tokenProvided)
                violations.Add("GREE_ALICE_MQTT_AUTH_MODE=token requires GREE_ALICE_MQTT_TOKEN.");
            if (passwordProvided)
                violations.Add("GREE_ALICE_MQTT_AUTH_MODE=token must not include GREE_ALICE_MQTT_PASSWORD in the first safe validation scaffold.");
        }
        else if (authModeNormalized == "signed")
        {
            violations.Add("GREE_ALICE_MQTT_AUTH_MODE=signed is reserved but not implemented in this validation scaffold.");
        }

        foreach (var unsafeInput in FindUnsafeInputs())
            violations.Add($"Unsafe input is present and must be removed before any future CONNECT-only work: {unsafeInput.Name}.");

        var validationStatus = violations.Count == 0
            ? "inputs-valid-connect-still-not-implemented"
            : "blocked-fail-closed";

        var summary = new Summary(
            ValidationStatus: validationStatus,
            InputsChecked: observations.Length,
            ProvidedInputs: observations.Count(static observation => observation.Provided),
            MissingRequiredInputs: observations.Count(static observation => observation.RequiredForFutureConnectOnlyTest && !observation.Provided),
            InvalidInputs: observations.Count(static observation => !observation.Valid),
            UnsafeInputsPresent: FindUnsafeInputs().Count,
            ViolationCount: violations.Count,
            MqttConnectImplementationIncluded: false,
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            RawCredentialsStored: false);

        return new Report(
            StageName,
            ModeName,
            timestampUtc,
            new OptionsSnapshot(
                RepositoryRoot: MaskPath(options.RepositoryRoot, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
            summary,
            observations,
            FindUnsafeInputs(),
            violations,
            BuildValidationRules(),
            BuildMaskingRules(),
            Safety.Empty,
            new[]
            {
                "This is offline input validation only.",
                "It does not open TCP, TLS, or MQTT connections.",
                "It does not send MQTT CONNECT, SUBSCRIBE, PUBLISH, or any device command.",
                "Even when inputs validate, MQTT CONNECT implementation remains absent and must be added only in a separate explicit safety stage."
            });
    }

    private static IReadOnlyList<InputSpec> BuildSpecs() =>
        new[]
        {
            new InputSpec("GREE_ALICE_MQTT_HOST", false, true, "mqtt-hk.gree.com", false, "hostname"),
            new InputSpec("GREE_ALICE_MQTT_PORT", false, true, "1994", false, "port"),
            new InputSpec("GREE_ALICE_MQTT_CLIENT_ID", true, false, null, true, "non-empty"),
            new InputSpec("GREE_ALICE_MQTT_USERNAME", true, false, null, true, "non-empty"),
            new InputSpec("GREE_ALICE_MQTT_PASSWORD", false, false, null, true, "optional-secret"),
            new InputSpec("GREE_ALICE_MQTT_TOKEN", false, false, null, true, "optional-secret"),
            new InputSpec("GREE_ALICE_MQTT_AUTH_MODE", true, false, null, false, "auth-mode"),
            new InputSpec("GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS", false, true, "30", false, "keep-alive"),
            new InputSpec("GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS", false, true, "10", false, "timeout"),
            new InputSpec("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK", false, true, "true", false, "true-only"),
            new InputSpec("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE", false, true, "false", false, "false-only"),
            new InputSpec("GREE_ALICE_MQTT_ALLOW_PUBLISH", false, true, "false", false, "false-only")
        };

    private static InputObservation Observe(InputSpec spec)
    {
        var raw = GetRaw(spec.Name);
        var provided = !string.IsNullOrWhiteSpace(raw);
        var effective = provided ? raw!.Trim() : spec.DefaultValue;
        var source = provided ? "environment" : spec.HasDefault ? "default" : "missing";

        var (valid, message) = Validate(spec, effective, provided);

        return new InputObservation(
            Name: spec.Name,
            RequiredForFutureConnectOnlyTest: spec.RequiredForFutureConnectOnlyTest,
            Secret: spec.Secret,
            Source: source,
            Provided: provided,
            HasDefault: spec.HasDefault,
            EffectiveValueMasked: MaskValue(spec, effective, provided),
            Valid: valid,
            ValidationMessage: message);
    }

    private static (bool Valid, string Message) Validate(InputSpec spec, string? effectiveValue, bool provided)
    {
        if (spec.RequiredForFutureConnectOnlyTest && string.IsNullOrWhiteSpace(effectiveValue))
            return (false, "missing required input");

        if (string.IsNullOrWhiteSpace(effectiveValue))
            return (true, "not provided");

        var value = effectiveValue.Trim();

        return spec.Shape switch
        {
            "hostname" => value.Contains(' ') || value.Contains('/') || value.Contains('\\')
                ? (false, "host must be a hostname without spaces or URL path")
                : (true, "ok"),

            "port" => int.TryParse(value, out var port) && port is >= 1 and <= 65535
                ? (true, "ok")
                : (false, "port must be an integer from 1 to 65535"),

            "non-empty" => string.IsNullOrWhiteSpace(value)
                ? (false, "value must be non-empty")
                : (true, "ok"),

            "optional-secret" => (true, provided ? "provided" : "not provided"),

            "auth-mode" => value.Equals("password", StringComparison.OrdinalIgnoreCase) ||
                           value.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                           value.Equals("signed", StringComparison.OrdinalIgnoreCase)
                ? (true, "ok")
                : (false, "auth mode must be one of: password, token, signed"),

            "keep-alive" => int.TryParse(value, out var keepAlive) && keepAlive is >= 5 and <= 300
                ? (true, "ok")
                : (false, "keep alive must be an integer from 5 to 300"),

            "timeout" => int.TryParse(value, out var timeout) && timeout is >= 1 and <= 60
                ? (true, "ok")
                : (false, "timeout must be an integer from 1 to 60"),

            "true-only" => value.Equals("true", StringComparison.OrdinalIgnoreCase)
                ? (true, "ok")
                : (false, "value must be true"),

            "false-only" => value.Equals("false", StringComparison.OrdinalIgnoreCase)
                ? (true, "ok")
                : (false, "value must be false"),

            _ => (false, $"unknown validation shape: {spec.Shape}")
        };
    }

    private static IReadOnlyList<UnsafeInputObservation> FindUnsafeInputs()
    {
        var unsafeNames = new[]
        {
            "GREE_ALICE_MQTT_TOPIC",
            "GREE_ALICE_MQTT_SUBSCRIBE_TOPIC",
            "GREE_ALICE_MQTT_PUBLISH_TOPIC",
            "GREE_ALICE_MQTT_WILDCARD_TOPIC",
            "GREE_ALICE_MQTT_COMMAND_PAYLOAD",
            "GREE_ALICE_MQTT_POWER",
            "GREE_ALICE_MQTT_MODE",
            "GREE_ALICE_MQTT_SETPOINT",
            "GREE_ALICE_MQTT_FAN",
            "GREE_ALICE_MQTT_SWING"
        };

        return unsafeNames
            .Where(static name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            .Select(static name => new UnsafeInputObservation(name, "present-masked", "not allowed in CONNECT-only validation scaffold"))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildValidationRules() =>
        new[]
        {
            "Fail closed when client id is missing.",
            "Fail closed when username is missing.",
            "Fail closed when auth mode is missing or not one of password/token/signed.",
            "Fail closed when password auth mode has no password.",
            "Fail closed when token auth mode has no token.",
            "Fail closed when signed auth mode is selected because signed auth is not implemented yet.",
            "Reject GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true.",
            "Reject GREE_ALICE_MQTT_ALLOW_PUBLISH=true.",
            "Require GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true.",
            "Reject topic inputs and command payload inputs."
        };

    private static IReadOnlyList<string> BuildMaskingRules() =>
        new[]
        {
            "Never print raw client id, username, password, token, or device key.",
            "For secrets, output only provided/missing and length bucket.",
            "For non-secret defaults, host/port/numeric/bool values may be printed.",
            "Unsafe topic or command inputs are reported by variable name only.",
            "All artifacts stay under artifacts/gree-alice/ and must not be committed."
        };

    private static string? GetRaw(string name) => Environment.GetEnvironmentVariable(name);

    private static string? MaskValue(InputSpec spec, string? value, bool provided)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!spec.Secret)
            return value;

        var length = value.Length;
        var bucket = length switch
        {
            <= 0 => "0",
            <= 4 => "1-4",
            <= 8 => "5-8",
            <= 16 => "9-16",
            <= 32 => "17-32",
            <= 64 => "33-64",
            _ => "65+"
        };

        return provided
            ? $"<provided:length-bucket:{bucket}>"
            : $"<default-secret:length-bucket:{bucket}>";
    }

    private static string? MaskPath(string? path, string repoRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var fullPath = Path.GetFullPath(path);
        var fullRepo = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (fullPath.StartsWith(fullRepo, StringComparison.OrdinalIgnoreCase))
            return "." + fullPath[fullRepo.Length..].Replace(Path.DirectorySeparatorChar, '/');

        return "<external-path>";
    }

    private static void PrintSummary(Report report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT CONNECT input validation");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Validation:");
        Console.WriteLine($"  Status: {report.Summary.ValidationStatus}");
        Console.WriteLine($"  Inputs checked: {report.Summary.InputsChecked}");
        Console.WriteLine($"  Provided inputs: {report.Summary.ProvidedInputs}");
        Console.WriteLine($"  Missing required inputs: {report.Summary.MissingRequiredInputs}");
        Console.WriteLine($"  Invalid inputs: {report.Summary.InvalidInputs}");
        Console.WriteLine($"  Unsafe inputs present: {report.Summary.UnsafeInputsPresent}");
        Console.WriteLine($"  Violations: {report.Summary.ViolationCount}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  MQTT CONNECT implementation included: {ToYesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {ToYesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {ToYesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {ToYesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {ToYesNo(report.Summary.DeviceControlSent)}");
        Console.WriteLine($"  Raw credentials stored: {ToYesNo(report.Summary.RawCredentialsStored)}");

        if (report.Violations.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Violations:");
            Console.ResetColor();

            foreach (var violation in report.Violations)
                Console.WriteLine($"  - {violation}");
        }

        Console.WriteLine();
        Console.WriteLine("Next step: do not implement CONNECT until validation contract and auth/client-id evidence are confirmed.");
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(string RepositoryRoot, string OutputDirectory, bool ConfigurationOnly)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-connect-input-validation");

            return new Options(
                repoRoot,
                Path.GetFullPath(outputDir),
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--validate-mqtt-connect-inputs", StringComparison.OrdinalIgnoreCase))
                {
                    result["validate-mqtt-connect-inputs"] = null;
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

    private sealed record Report(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        OptionsSnapshot Options,
        Summary Summary,
        IReadOnlyList<InputObservation> Inputs,
        IReadOnlyList<UnsafeInputObservation> UnsafeInputs,
        IReadOnlyList<string> Violations,
        IReadOnlyList<string> ValidationRules,
        IReadOnlyList<string> MaskingRules,
        Safety Safety,
        IReadOnlyList<string> Notes)
    {
        public static Report ConfigurationOnly(Options options, DateTimeOffset timestampUtc)
        {
            return new Report(
                StageName,
                "configuration-only",
                timestampUtc,
                new OptionsSnapshot(
                    RepositoryRoot: MaskPath(options.RepositoryRoot, options.RepositoryRoot),
                    OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
                new Summary(
                    ValidationStatus: "configuration-only",
                    InputsChecked: 0,
                    ProvidedInputs: 0,
                    MissingRequiredInputs: 0,
                    InvalidInputs: 0,
                    UnsafeInputsPresent: 0,
                    ViolationCount: 0,
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false,
                    RawCredentialsStored: false),
                Array.Empty<InputObservation>(),
                Array.Empty<UnsafeInputObservation>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Safety.Empty,
                new[]
                {
                    "Configuration-only mode did not read environment variables.",
                    "Run without --configuration-only to validate the local environment."
                });
        }
    }

    private sealed record OptionsSnapshot(string? RepositoryRoot, string? OutputDirectory);

    private sealed record Summary(
        string ValidationStatus,
        int InputsChecked,
        int ProvidedInputs,
        int MissingRequiredInputs,
        int InvalidInputs,
        int UnsafeInputsPresent,
        int ViolationCount,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored);

    private sealed record InputSpec(
        string Name,
        bool RequiredForFutureConnectOnlyTest,
        bool HasDefault,
        string? DefaultValue,
        bool Secret,
        string Shape);

    private sealed record InputObservation(
        string Name,
        bool RequiredForFutureConnectOnlyTest,
        bool Secret,
        string Source,
        bool Provided,
        bool HasDefault,
        string? EffectiveValueMasked,
        bool Valid,
        string ValidationMessage);

    private sealed record UnsafeInputObservation(
        string Name,
        string Status,
        string Reason);

    private sealed record Safety(
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored,
        bool RawDeviceKeysStored,
        bool RawMacsStored,
        bool ProductionBridgeChanged)
    {
        public static Safety Empty { get; } =
            new(false, false, false, false, false, false, false, false);
    }
}
