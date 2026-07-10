using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed record GreePlusCommandPayload(
    [property: JsonPropertyName("t")] string T,
    [property: JsonPropertyName("opt")] IReadOnlyList<string> Opt,
    [property: JsonPropertyName("p")] IReadOnlyList<object> P);
