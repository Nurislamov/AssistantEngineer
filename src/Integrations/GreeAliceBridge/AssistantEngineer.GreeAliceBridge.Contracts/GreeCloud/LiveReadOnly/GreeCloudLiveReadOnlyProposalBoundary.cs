namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly;

public static class GreeCloudLiveReadOnlyProposalBoundary
{
    public const bool LiveReadOnlyPilotApproved = false;
    public const bool LiveReadOnlyAdapterImplemented = false;
    public const bool LiveReadOnlyAdapterEnabled = false;
    public const bool LiveControlAllowed = false;
    public const bool MqttAllowed = false;
    public const bool ProductionWiringAllowed = false;
    public const string ApprovalStatus = GreeCloudLiveReadOnlyApprovalStatus.NotApproved;

    public static IReadOnlyList<string> AllowedReadFields { get; } =
    [
        GreeCloudLiveReadOnlyAllowedField.DeviceDescriptor,
        GreeCloudLiveReadOnlyAllowedField.OnlineState,
        GreeCloudLiveReadOnlyAllowedField.PowerState,
        GreeCloudLiveReadOnlyAllowedField.Mode,
        GreeCloudLiveReadOnlyAllowedField.TargetTemperature,
        GreeCloudLiveReadOnlyAllowedField.CurrentTemperature,
        GreeCloudLiveReadOnlyAllowedField.FanSpeed,
        GreeCloudLiveReadOnlyAllowedField.SwingState,
        GreeCloudLiveReadOnlyAllowedField.ErrorStatus
    ];

    public static IReadOnlyList<string> ForbiddenOperations { get; } =
    [
        GreeCloudLiveReadOnlyForbiddenOperation.PowerOnOff,
        GreeCloudLiveReadOnlyForbiddenOperation.SetMode,
        GreeCloudLiveReadOnlyForbiddenOperation.SetTemperature,
        GreeCloudLiveReadOnlyForbiddenOperation.SetFanSpeed,
        GreeCloudLiveReadOnlyForbiddenOperation.SetSwing,
        GreeCloudLiveReadOnlyForbiddenOperation.RunScene,
        GreeCloudLiveReadOnlyForbiddenOperation.ModifySchedule,
        GreeCloudLiveReadOnlyForbiddenOperation.BindDevice,
        GreeCloudLiveReadOnlyForbiddenOperation.UnbindDevice,
        GreeCloudLiveReadOnlyForbiddenOperation.AccountMutation,
        GreeCloudLiveReadOnlyForbiddenOperation.FirmwareUpdate,
        GreeCloudLiveReadOnlyForbiddenOperation.MqttConnect,
        GreeCloudLiveReadOnlyForbiddenOperation.MqttSubscribe,
        GreeCloudLiveReadOnlyForbiddenOperation.MqttPublish,
        GreeCloudLiveReadOnlyForbiddenOperation.ProductionDeploy
    ];

    public static IReadOnlyList<string> EvidenceRequirements { get; } =
    [
        GreeCloudLiveReadOnlyEvidenceRequirement.OperatorApprovedAccountDeviceScope,
        GreeCloudLiveReadOnlyEvidenceRequirement.ExternalAuthMaterialOutsideRepository,
        GreeCloudLiveReadOnlyEvidenceRequirement.ReadOnlyEndpointPath,
        GreeCloudLiveReadOnlyEvidenceRequirement.AllowedReadFields,
        GreeCloudLiveReadOnlyEvidenceRequirement.ControlMutationAbsence,
        GreeCloudLiveReadOnlyEvidenceRequirement.KillSwitchPlan,
        GreeCloudLiveReadOnlyEvidenceRequirement.RollbackPlan,
        GreeCloudLiveReadOnlyEvidenceRequirement.MaskedLogs
    ];
}
