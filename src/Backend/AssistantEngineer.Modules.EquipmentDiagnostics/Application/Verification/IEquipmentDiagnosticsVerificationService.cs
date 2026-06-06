namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

public interface IEquipmentDiagnosticsVerificationService
{
    EquipmentDiagnosticsVerificationReport Verify(EquipmentDiagnosticsVerificationInput input);
}
