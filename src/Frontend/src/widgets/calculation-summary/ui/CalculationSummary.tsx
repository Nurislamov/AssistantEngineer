import { Stack, Typography } from "@mui/material";
import type { CalculationResultDto } from "@/entities/calculation/types";
import { formatNumber } from "@/shared/lib/format";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface CalculationSummaryProps {
  result?: CalculationResultDto;
}

export function CalculationSummary({ result }: CalculationSummaryProps): JSX.Element {
  if (!result) {
    return (
      <EmptyState
        title="Последнего результата нет"
        description="Запустите расчёт, чтобы увидеть значения здесь."
      />
    );
  }

  return (
    <DataCard>
      <Stack spacing={1.5}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          Последний расчёт
        </Typography>
        <Typography variant="body2">Теплопотери: {formatNumber(result.totalHeatLoss)} Вт</Typography>
        <Typography variant="body2">Теплопритоки: {formatNumber(result.totalHeatGain)} Вт</Typography>
        <Typography variant="body2">Отопление: {formatNumber(result.heatingLoad)} Вт</Typography>
        <Typography variant="body2">Охлаждение: {formatNumber(result.coolingLoad)} Вт</Typography>
      </Stack>
    </DataCard>
  );
}
