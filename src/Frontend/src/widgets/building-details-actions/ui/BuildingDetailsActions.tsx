import AddIcon from "@mui/icons-material/Add";
import LayersIcon from "@mui/icons-material/Layers";
import { Button, Stack } from "@mui/material";
import { RunBuildingCalculationButton } from "@/features/calculations/run-building-calculation/ui/RunBuildingCalculationButton";
import { DownloadBuildingReportButton } from "@/features/reports/report-download/ui/DownloadBuildingReportButton";

interface BuildingDetailsActionsProps {
  buildingId: number;
  onAddFloor: () => void;
  onAddRoom: () => void;
  onOperationError: (message: string) => void;
}

export function BuildingDetailsActions({
  buildingId,
  onAddFloor,
  onAddRoom,
  onOperationError,
}: BuildingDetailsActionsProps): JSX.Element {
  return (
    <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
      <Button variant="outlined" startIcon={<LayersIcon />} onClick={onAddFloor}>
        Добавить этаж
      </Button>
      <Button variant="outlined" startIcon={<AddIcon />} onClick={onAddRoom}>
        Добавить помещение
      </Button>
      <DownloadBuildingReportButton buildingId={buildingId} onError={onOperationError} />
      <RunBuildingCalculationButton buildingId={buildingId} onError={onOperationError} />
    </Stack>
  );
}
