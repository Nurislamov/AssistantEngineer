import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { EquipmentPanel } from "@/widgets/building-workspace/ui/BuildingWorkspace";

export function EquipmentSelectionPage(): JSX.Element {
  return (
    <PageContainer>
      <PageHeader
        title="Equipment"
        description="Manage the cooling equipment catalog. Room selection is available inside each building workspace."
      />
      <EquipmentPanel rooms={[]} />
    </PageContainer>
  );
}
