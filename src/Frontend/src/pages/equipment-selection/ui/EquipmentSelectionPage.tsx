import { EquipmentSelectionPlaceholder } from "@/features/equipment/equipment-selection/ui/EquipmentSelectionPlaceholder";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";

export function EquipmentSelectionPage(): JSX.Element {
  return (
    <PageContainer>
      <PageHeader
        title="Подбор оборудования"
        description="Здесь будет подключаться подбор оборудования по результатам расчётов."
      />
      <EquipmentSelectionPlaceholder />
    </PageContainer>
  );
}
