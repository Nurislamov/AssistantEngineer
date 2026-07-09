namespace AssistantEngineer.GreeAliceBridge.Contracts;

public static class GreeAliceBridgeSafetyBoundary
{
    public const bool LiveMqttConnectEnabled = false;
    public const bool MqttSubscribeEnabled = false;
    public const bool MqttPublishEnabled = false;
    public const bool DeviceControlEnabled = false;
    public const bool GreeRuntimeControlEnabled = false;
    public const string RuntimeMode = "offline-fixture";
}
