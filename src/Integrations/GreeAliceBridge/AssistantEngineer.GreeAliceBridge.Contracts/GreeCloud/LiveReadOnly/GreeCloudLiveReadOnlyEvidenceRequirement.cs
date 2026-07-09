namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly;

public static class GreeCloudLiveReadOnlyEvidenceRequirement
{
    public const string OperatorApprovedAccountDeviceScope = "operator_approved_account_device_scope";
    public const string ExternalAuthMaterialOutsideRepository = "external_auth_material_outside_repository";
    public const string ReadOnlyEndpointPath = "read_only_endpoint_path";
    public const string AllowedReadFields = "allowed_read_fields";
    public const string ControlMutationAbsence = "control_mutation_absence";
    public const string KillSwitchPlan = "kill_switch_plan";
    public const string RollbackPlan = "rollback_plan";
    public const string MaskedLogs = "masked_logs";
}
