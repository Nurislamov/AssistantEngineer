namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportDraft(
    GreeAliceRegistryImportAccountDraft Account,
    GreeAliceRegistryImportHomeDraft Home,
    IReadOnlyList<GreeAliceRegistryImportRoomDraft> Rooms,
    IReadOnlyList<GreeAliceRegistryImportDeviceDraft> Devices,
    IReadOnlyList<GreeAliceRegistryImportVrfGatewayDraft> VrfGateways,
    IReadOnlyList<GreeAliceRegistryImportVrfChildUnitDraft> VrfChildUnits,
    IReadOnlyList<GreeAliceRegistryImportExposureDecision> ExposureDecisions,
    string ImportMode);
