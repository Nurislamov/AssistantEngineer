import { Stack, Typography } from "@mui/material";
import type { BuildingDto } from "@/entities/building/types";
import { DataCard } from "@/shared/ui/DataCard";

interface BuildingSummaryProps {
  building: BuildingDto;
}

export function BuildingSummary({ building }: BuildingSummaryProps): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={1}>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {building.name}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          ID здания: {building.id}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Проект: {building.projectId}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Климатическая зона: {building.climateZoneName ?? building.climateZoneId ?? "-"}
        </Typography>
      </Stack>
    </DataCard>
  );
}
