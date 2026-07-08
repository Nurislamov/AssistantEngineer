using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttConnectDryRunCommand
{
    private const string StageName = "GREE-ALICE-19";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] ContractInputs =
    {
        "GREE_ALICE_MQTT_HOST",
        "GREE_ALICE_MQTT_PORT",
        "GREE_ALICE_MQTT_CLIENT_ID",
        "GREE_ALICE_MQTT_USERNAME",
        "GREE_ALICE_MQTT_PASSWORD",
        "GREE_ALICE_MQTT_TOKEN",
        "GREE_ALICE_MQTT_AUTH_MODE",
        "GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS",
        "GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS",
        "GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK",
        "GREE_ALICE_MQTT_ALLOW_SUBSCRIBE",
        "GREE_ALICE_MQTT_ALLOW_PUBLISH"
    };

    private static readonly string[] RequiredInputs =
    {
        "GREE_ALICE_MQTT_CLIENT_ID",
        "GREE_ALICE_MQTT_USERNAME",
        "GREE_ALICE_MQTT_AUTH_MODE"
    };

    private static readonly string[] SecretInputs =
    {
        "GREE_ALICE_MQTT_PASSWORD",
        "GREE_ALICE_MQTT_TOKEN"
    };

    private static readonly string[] ForbiddenArgumentNames =
    {
        "topic",
        "payload",
        "command",
        "cmd",
        "power",
        "pow",
        "setpoint",
        "set-tem",
        "settem",
        "mode",
        "fan",
        "swing"
    };

    public static int Run(string[] args)
    {
        var options = Options.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestamp = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? DryRunReport.ConfigurationOnly(options, timestamp)
            : BuildReport(options, timestamp);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-connect-dry-run-{timestamp:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        Print(report, outputPath);

        return 0;
    }

    private static DryRunReport BuildReport(Options options, DateTimeOffset timestamp)
    {
        var inputs = ContractInputs
            .Select(name => BuildInputSummary(name, Environment.GetEnvironmentVariable(name)))
            .ToArray();

        var missingRequired = RequiredInputs
            .Where(name => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            .ToArray();

        var violations = new List<string>();
        var unsafeInputs = new List<string>();

        foreach (var missing in missingRequired)
            violations.Add($"{missing} is required for any future CONNECT-only live safety stage.");

        var authMode = Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_AUTH_MODE");
        if (!string.IsNullOrWhiteSpace(authMode) && !IsAllowedAuthMode(authMode))
            violations.Add("GREE_ALICE_MQTT_AUTH_MODE must be one of: password, token, signature.");

        ValidateIntegerRange("GREE_ALICE_MQTT_PORT", Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_PORT"), 1, 65535, violations);
        ValidateIntegerRange("GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS", Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS"), 5, 3600, violations);
        ValidateIntegerRange("GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS", Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS"), 1, 60, violations);

        ValidateBooleanIfPresent("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK", Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK"), violations);
        ValidateBooleanIfPresent("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE", Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE"), violations);
        ValidateBooleanIfPresent("GREE_ALICE_MQTT_ALLOW_PUBLISH", Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_ALLOW_PUBLISH"), violations);

        if (IsExplicitFalse(Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK")))
        {
            violations.Add("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=false is unsafe for CONNECT-only dry-run.");
            unsafeInputs.Add("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK");
        }

        if (IsExplicitTrue(Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE")))
        {
            violations.Add("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true is forbidden for CONNECT-only dry-run.");
            unsafeInputs.Add("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE");
        }

        if (IsExplicitTrue(Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_ALLOW_PUBLISH")))
        {
            violations.Add("GREE_ALICE_MQTT_ALLOW_PUBLISH=true is forbidden for CONNECT-only dry-run.");
            unsafeInputs.Add("GREE_ALICE_MQTT_ALLOW_PUBLISH");
        }

        foreach (var forbidden in options.ForbiddenArguments)
        {
            violations.Add($"Forbidden argument was provided: --{forbidden}");
            unsafeInputs.Add($"--{forbidden}");
        }

        var status = violations.Count == 0
            ? "dry-run-ready-for-separate-live-safety-stage"
            : "blocked-fail-closed";

        var summary = new DryRunSummary(
            InputsChecked: ContractInputs.Length,
            ProvidedInputs: inputs.Count(static item => item.Provided),
            MissingRequiredInputs: missingRequired.Length,
            InvalidInputs: Math.Max(0, violations.Count - unsafeInputs.Count),
            UnsafeInputsPresent: unsafeInputs.Count,
            ForbiddenArgumentsPresent: options.ForbiddenArguments.Count,
            Status: status,
            ConnectGate: "blocked",
            SubscribeGate: "blocked",
            PublishGate: "blocked",
            DeviceControlGate: "blocked",
            OutputContainsRawValues: false,
            NetworkConnectionOpened: false,
            MqttConnectImplementationIncluded: false,
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            RawCredentialsStored: false);

        return new DryRunReport(
            StageName,
            "mqtt-connect-dry-run-contract",
            timestamp,
            new DryRunInputs(MaskPath(options.OutputDirectory, options.RepositoryRoot), false),
            summary,
            inputs,
            RequiredInputs,
            SecretInputs,
            violations,
            new[]
            {
                "This is an offline dry-run contract only.",
                "No DNS, TCP, TLS, or MQTT network operation is performed.",
                "No MQTT CONNECT, SUBSCRIBE, PUBLISH, or device control command is sent.",
                "Raw environment variable values are not written to console or report."
            });
    }

    private static InputSummary BuildInputSummary(string name, string? value)
    {
        var provided = !string.IsNullOrWhiteSpace(value);
        return new InputSummary(
            Name: name,
            Provided: provided,
            Required: RequiredInputs.Contains(name, StringComparer.Ordinal),
            Secret: SecretInputs.Contains(name, StringComparer.Ordinal),
            LengthBucket: provided ? GetLengthBucket(value!.Length) : null,
            Value: provided ? "<masked>" : null);
    }

    private static bool IsAllowedAuthMode(string value) =>
        value.Equals("password", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("token", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("signature", StringComparison.OrdinalIgnoreCase);

    private static void ValidateIntegerRange(string name, string? value, int min, int max, List<string> violations)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!int.TryParse(value, out var parsed) || parsed < min || parsed > max)
            violations.Add($"{name} must be an integer in range {min}..{max}.");
    }

    private static void ValidateBooleanIfPresent(string name, string? value, List<string> violations)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!bool.TryParse(value, out _))
            violations.Add($"{name} must be true or false.");
    }

    private static bool IsExplicitTrue(string? value) => bool.TryParse(value, out var parsed) && parsed;
    private static bool IsExplicitFalse(string? value) => bool.TryParse(value, out var parsed) && !parsed;

    private static string GetLengthBucket(int length) =>
        length switch
        {
            <= 0 => "0",
            <= 4 => "1-4",
            <= 8 => "5-8",
            <= 16 => "9-16",
            <= 32 => "17-32",
            <= 64 => "33-64",
            <= 128 => "65-128",
            <= 256 => "129-256",
            _ => "257+"
        };

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

    private static void Print(DryRunReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT CONNECT dry-run contract");
        Console.WriteLine($"Stage: {report.Stage}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Validation:");
        Console.WriteLine($"  Status: {report.Summary.Status}");
        Console.WriteLine($"  Inputs checked: {report.Summary.InputsChecked}");
        Console.WriteLine($"  Provided inputs: {report.Summary.ProvidedInputs}");
        Console.WriteLine($"  Missing required inputs: {report.Summary.MissingRequiredInputs}");
        Console.WriteLine($"  Invalid inputs: {report.Summary.InvalidInputs}");
        Console.WriteLine($"  Unsafe inputs present: {report.Summary.UnsafeInputsPresent}");
        Console.WriteLine($"  Forbidden arguments present: {report.Summary.ForbiddenArgumentsPresent}");
        Console.WriteLine();

        Console.WriteLine("Gates:");
        Console.WriteLine($"  CONNECT gate: {report.Summary.ConnectGate}");
        Console.WriteLine($"  SUBSCRIBE gate: {report.Summary.SubscribeGate}");
        Console.WriteLine($"  PUBLISH gate: {report.Summary.PublishGate}");
        Console.WriteLine($"  Device control gate: {report.Summary.DeviceControlGate}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  Output contains raw values: {YesNo(report.Summary.OutputContainsRawValues)}");
        Console.WriteLine($"  Network connection opened: {YesNo(report.Summary.NetworkConnectionOpened)}");
        Console.WriteLine($"  MQTT CONNECT implementation included: {YesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {YesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {YesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {YesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {YesNo(report.Summary.DeviceControlSent)}");
        Console.WriteLine($"  Raw credentials stored: {YesNo(report.Summary.RawCredentialsStored)}");

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
        Console.WriteLine("Next step: use dry-run output only; live CONNECT remains a separate explicit safety stage.");
    }

    private static string YesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(
        string RepositoryRoot,
        string OutputDirectory,
        bool ConfigurationOnly,
        IReadOnlyList<string> ForbiddenArguments)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = Get(values, "repo-root") ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var outputDir = Get(values, "output-dir") ??
                Environment.GetEnvironmentVariable("GREE_ALICE_OUTPUT_DIR") ??
                Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-connect-dry-run");

            var forbiddenArgs = values.Keys
                .Where(key => ForbiddenArgumentNames.Contains(key, StringComparer.OrdinalIgnoreCase))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new Options(
                repoRoot,
                Path.GetFullPath(outputDir),
                values.ContainsKey("configuration-only"),
                forbiddenArgs);
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.Equals("--mqtt-connect-dry-run", StringComparison.OrdinalIgnoreCase))
                {
                    result["mqtt-connect-dry-run"] = null;
                    continue;
                }

                if (!arg.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unexpected argument: {arg}");

                var key = arg[2..];

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    result[key] = args[i + 1];
                    i++;
                }
                else
                {
                    result[key] = null;
                }
            }

            return result;
        }

        private static string? Get(IReadOnlyDictionary<string, string?> values, string key) =>
            values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;

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

    private sealed record DryRunReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        DryRunInputs Inputs,
        DryRunSummary Summary,
        IReadOnlyList<InputSummary> InputSummaries,
        IReadOnlyList<string> RequiredInputs,
        IReadOnlyList<string> SecretInputs,
        IReadOnlyList<string> Violations,
        IReadOnlyList<string> Notes)
    {
        public static DryRunReport ConfigurationOnly(Options options, DateTimeOffset timestamp)
        {
            return new DryRunReport(
                StageName,
                "configuration-only",
                timestamp,
                new DryRunInputs(MaskPath(options.OutputDirectory, options.RepositoryRoot), true),
                new DryRunSummary(
                    InputsChecked: ContractInputs.Length,
                    ProvidedInputs: 0,
                    MissingRequiredInputs: 0,
                    InvalidInputs: 0,
                    UnsafeInputsPresent: 0,
                    ForbiddenArgumentsPresent: 0,
                    Status: "configuration-only",
                    ConnectGate: "not-evaluated",
                    SubscribeGate: "not-evaluated",
                    PublishGate: "not-evaluated",
                    DeviceControlGate: "not-evaluated",
                    OutputContainsRawValues: false,
                    NetworkConnectionOpened: false,
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false,
                    RawCredentialsStored: false),
                Array.Empty<InputSummary>(),
                MqttConnectDryRunCommand.RequiredInputs,
                MqttConnectDryRunCommand.SecretInputs,
                Array.Empty<string>(),
                new[] { "Configuration-only mode did not inspect environment variables." });
        }
    }

    private sealed record DryRunInputs(string? OutputDirectory, bool ConfigurationOnly);

    private sealed record DryRunSummary(
        int InputsChecked,
        int ProvidedInputs,
        int MissingRequiredInputs,
        int InvalidInputs,
        int UnsafeInputsPresent,
        int ForbiddenArgumentsPresent,
        string Status,
        string ConnectGate,
        string SubscribeGate,
        string PublishGate,
        string DeviceControlGate,
        bool OutputContainsRawValues,
        bool NetworkConnectionOpened,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored);

    private sealed record InputSummary(
        string Name,
        bool Provided,
        bool Required,
        bool Secret,
        string? LengthBucket,
        string? Value);
}
