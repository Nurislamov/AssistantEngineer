import { Alert, Stack, Typography } from "@mui/material";
import { DataCard } from "@/shared/ui/DataCard";

export function EquipmentSelectionPlaceholder(): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={1.5}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          Подбор оборудования
        </Typography>
        <Alert severity="info">
          Модуль зарезервирован под будущую интеграцию расчётных нагрузок с каталогом оборудования.
        </Alert>
      </Stack>
    </DataCard>
  );
}
