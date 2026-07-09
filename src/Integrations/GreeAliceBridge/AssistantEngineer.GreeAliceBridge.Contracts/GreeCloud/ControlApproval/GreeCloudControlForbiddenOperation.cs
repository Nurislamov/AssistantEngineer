namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

public static class GreeCloudControlForbiddenOperation
{
    public const string FirmwareUpdate = "firmware_update";
    public const string DeviceBinding = "device_binding";
    public const string DeviceUnbinding = "device_unbinding";
    public const string AccountMutation = "account_mutation";
    public const string ScheduleMutation = "schedule_mutation";
    public const string TimerMutation = "timer_mutation";
    public const string SceneExecution = "scene_execution";
    public const string BulkControl = "bulk_control";
    public const string MqttConnect = "mqtt_connect";
    public const string MqttSubscribe = "mqtt_subscribe";
    public const string MqttPublish = "mqtt_publish";
    public const string ProductionDeploy = "production_deploy";
}
