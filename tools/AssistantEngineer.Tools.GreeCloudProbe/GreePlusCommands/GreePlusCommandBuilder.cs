namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public static class GreePlusCommandBuilder
{
    private const string CommandType = "cmd";

    public static GreePlusCommandPayload PowerOn()
    {
        return Payload(["Pow", "WdSpd", "Quiet", "Tur"], [1, 0, 0, 0]);
    }

    public static GreePlusCommandPayload PowerOff(int setTem = 25)
    {
        ValidateTemperature(setTem);

        return Payload(
            ["TemUn", "SetTem", "TemRec", "Pow", "SwhSlp", "SlpMod", "Air"],
            [0, setTem, 0, 0, 0, 0, 0]);
    }

    public static GreePlusCommandPayload SetTemperature(int setTem)
    {
        ValidateTemperature(setTem);

        return Payload(["SetTem", "TemUn", "TemRec"], [setTem, 0, 0]);
    }

    public static GreePlusCommandPayload SetMode(GreePlusMode mode, int setTem = 25)
    {
        ValidateTemperature(setTem);

        return mode switch
        {
            GreePlusMode.Auto => Payload(["Dmod", "Dwet", "Mod"], [15, 0, 0]),
            GreePlusMode.Cool => Payload(["Dmod", "Dwet", "Mod", "SetTem", "TemRec"], [15, 0, 1, setTem, 0]),
            GreePlusMode.Dry => Payload(["AssHt", "Dmod", "Dwet", "Mod", "WdSpd", "Quiet", "Tur"], [0, 15, 0, 2, 1, 0, 0]),
            GreePlusMode.Fan => Payload(["Dmod", "Dwet", "Mod"], [15, 0, 3]),
            GreePlusMode.Heat => Payload(["Dmod", "Dwet", "Mod"], [15, 0, 4]),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Gree Plus mode.")
        };
    }

    public static GreePlusCommandPayload SetFan(GreePlusFanSpeed fan)
    {
        return fan switch
        {
            GreePlusFanSpeed.Auto => Payload(["WdSpd", "Quiet", "Tur"], [0, 0, 0]),
            GreePlusFanSpeed.Low => Payload(["WdSpd", "Quiet", "Tur"], [1, 0, 0]),
            GreePlusFanSpeed.MediumLow => Payload(["WdSpd", "Quiet", "Tur"], [2, 0, 0]),
            GreePlusFanSpeed.Medium => Payload(["WdSpd", "Quiet", "Tur"], [3, 0, 0]),
            GreePlusFanSpeed.MediumHigh => Payload(["WdSpd", "Quiet", "Tur"], [4, 0, 0]),
            GreePlusFanSpeed.High => Payload(["WdSpd", "Quiet", "Tur"], [5, 0, 0]),
            GreePlusFanSpeed.Quiet => Payload(["Quiet", "Tur"], [2, 0]),
            GreePlusFanSpeed.Turbo => Payload(["Quiet", "Tur"], [0, 1]),
            _ => throw new ArgumentOutOfRangeException(nameof(fan), fan, "Unsupported Gree Plus fan speed.")
        };
    }

    public static GreePlusCommandPayload SetFeature(GreePlusFeature feature, bool enabled)
    {
        return feature switch
        {
            GreePlusFeature.Light => Payload(["Lig"], [enabled ? 1 : 0]),
            GreePlusFeature.Blow => Payload(["Blo"], [enabled ? 1 : 0]),
            GreePlusFeature.Health => Payload(["Health"], [enabled ? 1 : 0]),
            GreePlusFeature.EnergySave when enabled => Payload(["WdSpd", "Quiet", "Tur", "SvSt"], [0, 0, 0, 1]),
            GreePlusFeature.EnergySave => Payload(["SvSt"], [0]),
            GreePlusFeature.Sleep when enabled => Payload(["SvSt", "Dmod", "SlpMod", "SwhSlp"], [0, 15, 1, 1]),
            GreePlusFeature.Sleep => Payload(["Dmod", "SlpMod", "SwhSlp"], [15, 0, 0]),
            _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, "Unsupported Gree Plus feature.")
        };
    }

    public static GreePlusCommandPayload SetVerticalSwing(GreePlusSwingPosition position)
    {
        return Payload(["SwUpDn"], [SwingValue(position)]);
    }

    public static GreePlusCommandPayload SetHorizontalSwing(GreePlusSwingPosition position)
    {
        return Payload(["SwingLfRig"], [SwingValue(position)]);
    }

    private static int SwingValue(GreePlusSwingPosition position)
    {
        return position switch
        {
            GreePlusSwingPosition.Swing => 1,
            GreePlusSwingPosition.Angle1 => 2,
            GreePlusSwingPosition.Angle2 => 3,
            GreePlusSwingPosition.Angle3 => 4,
            GreePlusSwingPosition.Angle4 => 5,
            GreePlusSwingPosition.Angle5 => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Unsupported Gree Plus swing position.")
        };
    }

    private static void ValidateTemperature(int setTem)
    {
        if (setTem is < 16 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(setTem), setTem, "Gree Plus set temperature must be in range 16..30.");
        }
    }

    private static GreePlusCommandPayload Payload(string[] opt, object[] p)
    {
        return new GreePlusCommandPayload(CommandType, opt, p);
    }
}
