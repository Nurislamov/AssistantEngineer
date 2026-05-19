using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

public sealed class OwnershipBackfillStagingRunHashCalculator
{
    public string Compute(
        string applyInputHash,
        string planHash,
        string signoffId,
        string readinessId,
        string stagingPreflightReference,
        JsonDocument applyResult,
        JsonDocument postApplyDryRun,
        JsonDocument postApplyGateResult,
        string rollbackEvidenceReference,
        string tenantIsolationMatrixReference,
        string regressionTestReference,
        string rulesetVersion)
    {
        var canonical = new StringBuilder();
        canonical.Append("stage=P6-12;");
        canonical.Append("ruleset=").Append(rulesetVersion).Append(';');
        canonical.Append("applyInputHash=").Append(applyInputHash).Append(';');
        canonical.Append("planHash=").Append(planHash).Append(';');
        canonical.Append("signoffId=").Append(signoffId).Append(';');
        canonical.Append("readinessId=").Append(readinessId).Append(';');
        canonical.Append("stagingPreflightRef=").Append(stagingPreflightReference).Append(';');
        canonical.Append("rollbackEvidenceRef=").Append(rollbackEvidenceReference).Append(';');
        canonical.Append("tenantIsolationRef=").Append(tenantIsolationMatrixReference).Append(';');
        canonical.Append("regressionRef=").Append(regressionTestReference).Append(';');

        canonical.Append("applyResult=").Append(ToCanonicalJson(applyResult.RootElement)).Append(';');
        canonical.Append("postApplyDryRun=").Append(ToCanonicalJson(postApplyDryRun.RootElement)).Append(';');
        canonical.Append("postApplyGateResult=").Append(ToCanonicalJson(postApplyGateResult.RootElement)).Append(';');

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
