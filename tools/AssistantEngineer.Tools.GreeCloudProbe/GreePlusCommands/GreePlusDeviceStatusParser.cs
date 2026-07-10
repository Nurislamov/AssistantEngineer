using System.Globalization;
using System.Text.Json;

namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public static class GreePlusDeviceStatusParser
{
    public static GreePlusDeviceStatusSnapshot Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new GreePlusDeviceStatusParsingException("Gree Plus status JSON must be a non-empty object.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new GreePlusDeviceStatusParsingException("Gree Plus status JSON root must be an object.");
            }

            JsonElement root = document.RootElement;

            return new GreePlusDeviceStatusSnapshot(
                Pow: ReadInt(root, "Pow"),
                Mod: ReadInt(root, "Mod"),
                TemUn: ReadInt(root, "TemUn"),
                SetTem: ReadInt(root, "SetTem"),
                TemRec: ReadInt(root, "TemRec"),
                WdSpd: ReadInt(root, "WdSpd"),
                Quiet: ReadInt(root, "Quiet"),
                Tur: ReadInt(root, "Tur"),
                SwUpDn: ReadInt(root, "SwUpDn"),
                SwingLfRig: ReadInt(root, "SwingLfRig"),
                Air: ReadInt(root, "Air"),
                Blo: ReadInt(root, "Blo"),
                Health: ReadInt(root, "Health"),
                SvSt: ReadInt(root, "SvSt"),
                Lig: ReadInt(root, "Lig"),
                SwhSlp: ReadInt(root, "SwhSlp"),
                SlpMod: ReadInt(root, "SlpMod"),
                Dmod: ReadInt(root, "Dmod"),
                Dwet: ReadInt(root, "Dwet"),
                AllErr: ReadInt(root, "AllErr"),
                DeviceState: ReadInt(root, "deviceState"),
                Status: ReadBool(root, "status"),
                Mid: ReadInt(root, "mid"),
                Host: ReadString(root, "host"));
        }
        catch (JsonException exception)
        {
            throw new GreePlusDeviceStatusParsingException("Gree Plus status JSON is malformed.", exception);
        }
    }

    public static bool TryParse(string? json, out GreePlusDeviceStatusSnapshot? snapshot)
    {
        try
        {
            snapshot = Parse(json);
            return true;
        }
        catch (GreePlusDeviceStatusParsingException)
        {
            snapshot = null;
            return false;
        }
    }

    private static int? ReadInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out int number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) => number,
            _ => null
        };
    }

    private static bool? ReadBool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out bool boolean) => boolean,
            JsonValueKind.Number when value.TryGetInt32(out int number) && number is 0 or 1 => number == 1,
            _ => null
        };
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
