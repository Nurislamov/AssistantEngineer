using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionHashCalculator
{
    public string Compute(
        JsonDocument stagingAcceptance,
        JsonDocument productionDryRunSummary,
        JsonDocument productionGateResult,
        JsonDocument productionPlan,
        JsonDocument productionSignoff,
        JsonDocument productionReadiness,
        JsonDocument productionPreviousValues,
        string productionChangeRequestId,
        string rulesetVersion)
    {
        var canonical = new StringBuilder();
        canonical.Append("stage=P6-13;");
        canonical.Append("ruleset=").Append(rulesetVersion).Append(';');
        canonical.Append("productionChangeRequestId=").Append(productionChangeRequestId).Append(';');

        canonical.Append("stagingAcceptance=").Append(ToCanonicalJson(stagingAcceptance.RootElement)).Append(';');
        canonical.Append("productionDryRun=").Append(ToCanonicalJson(productionDryRunSummary.RootElement)).Append(';');
        canonical.Append("productionGate=").Append(ToCanonicalJson(productionGateResult.RootElement)).Append(';');
        canonical.Append("productionPlan=").Append(ToCanonicalJson(productionPlan.RootElement)).Append(';');
        canonical.Append("productionSignoff=").Append(ToCanonicalJson(productionSignoff.RootElement)).Append(';');
        canonical.Append("productionReadiness=").Append(ToCanonicalJson(productionReadiness.RootElement)).Append(';');
        canonical.Append("productionPreviousValues=").Append(ToCanonicalJson(productionPreviousValues.RootElement)).Append(';');

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ToCanonicalJson(JsonElement element)
    {
        var builder = new StringBuilder();
        AppendCanonical(element, builder);
        return builder.ToString();
    }

    private static void AppendCanonical(JsonElement element, StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                builder.Append('{');
                var properties = element.EnumerateObject()
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .ToArray();

                for (var index = 0; index < properties.Length; index++)
                {
                    if (index > 0)
                        builder.Append(',');

                    builder.Append(JsonSerializer.Serialize(properties[index].Name));
                    builder.Append(':');
                    AppendCanonical(properties[index].Value, builder);
                }

                builder.Append('}');
                break;
            }
            case JsonValueKind.Array:
            {
                builder.Append('[');
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (index++ > 0)
                        builder.Append(',');

                    AppendCanonical(item, builder);
                }

                builder.Append(']');
                break;
            }
            case JsonValueKind.String:
                builder.Append(JsonSerializer.Serialize(element.GetString()));
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                builder.Append(element.GetRawText());
                break;
            default:
                builder.Append(JsonSerializer.Serialize(element.GetRawText()));
                break;
        }
    }
}
