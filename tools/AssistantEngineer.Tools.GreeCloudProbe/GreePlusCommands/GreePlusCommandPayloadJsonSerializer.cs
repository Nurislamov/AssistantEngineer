using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public static class GreePlusCommandPayloadJsonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(GreePlusCommandPayload payload)
    {
        Validate(payload);

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static void Validate(GreePlusCommandPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!string.Equals(payload.T, "cmd", StringComparison.Ordinal))
        {
            throw new ArgumentException("Gree Plus command payload type must be cmd.", nameof(payload));
        }

        ArgumentNullException.ThrowIfNull(payload.Opt);
        ArgumentNullException.ThrowIfNull(payload.P);

        if (payload.Opt.Count == 0 || payload.P.Count == 0)
        {
            throw new ArgumentException("Gree Plus command payload opt and p collections must not be empty.", nameof(payload));
        }

        if (payload.Opt.Count != payload.P.Count)
        {
            throw new ArgumentException("Gree Plus command payload opt and p counts must match.", nameof(payload));
        }

        if (payload.Opt.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Gree Plus command payload opt entries must be non-empty.", nameof(payload));
        }

        if (payload.P.Any(static value => value is null))
        {
            throw new ArgumentException("Gree Plus command payload p entries must be non-null.", nameof(payload));
        }
    }
}
