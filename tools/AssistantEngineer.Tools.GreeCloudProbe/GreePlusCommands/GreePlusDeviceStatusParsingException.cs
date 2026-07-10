namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed class GreePlusDeviceStatusParsingException : Exception
{
    public GreePlusDeviceStatusParsingException(string message)
        : base(message)
    {
    }

    public GreePlusDeviceStatusParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
