import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { EquipmentDiagnosticBotPanel } from "@/widgets/equipment-diagnostics/ui/EquipmentDiagnosticBotPanel";

export function EquipmentDiagnosticsPage(): JSX.Element {
  return (
    <PageContainer>
      <PageHeader
        title="Equipment diagnostics"
        description="Deterministic runtime-catalog guidance with explicit verification and safety boundaries."
      />
      <EquipmentDiagnosticBotPanel />
    </PageContainer>
  );
}
