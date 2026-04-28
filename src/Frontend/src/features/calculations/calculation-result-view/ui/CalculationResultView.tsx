import {
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import type { CalculationResultDto } from "@/entities/calculation/types";
import { formatDateTime, formatNumber } from "@/shared/lib/format";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface CalculationResultViewProps {
  result?: CalculationResultDto;
}

export function CalculationResultView({ result }: CalculationResultViewProps): JSX.Element {
  if (!result) {
    return (
      <EmptyState
        title="Результат расчёта не найден"
        description="Запустите расчёт со страницы здания или помещения."
      />
    );
  }

  return (
    <Stack spacing={3}>
      <DataCard>
        <Stack spacing={2}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {result.buildingName ?? result.roomName ?? "Результат расчёта"}
          </Typography>
          <Stack direction={{ xs: "column", md: "row" }} spacing={3}>
            <Metric label="Теплопотери, Вт" value={result.totalHeatLoss} />
            <Metric label="Теплопритоки, Вт" value={result.totalHeatGain} />
            <Metric label="Отопительная нагрузка, Вт" value={result.heatingLoad} />
            <Metric label="Холодильная нагрузка, Вт" value={result.coolingLoad} />
            <Metric label="Дата" value={formatDateTime(result.calculatedAt)} />
          </Stack>
        </Stack>
      </DataCard>

      {result.rooms && result.rooms.length > 0 ? (
        <DataCard>
          <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>
            Помещения
          </Typography>
          <TableContainer>
            <Table size="medium">
              <TableHead>
                <TableRow>
                  <TableCell>Помещение</TableCell>
                  <TableCell align="right">Теплопотери, Вт</TableCell>
                  <TableCell align="right">Теплопритоки, Вт</TableCell>
                  <TableCell align="right">Отопление, Вт</TableCell>
                  <TableCell align="right">Охлаждение, Вт</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {result.rooms.map((room) => (
                  <TableRow key={room.roomId} hover>
                    <TableCell>{room.roomName}</TableCell>
                    <TableCell align="right">{formatNumber(room.heatLoss)}</TableCell>
                    <TableCell align="right">{formatNumber(room.heatGain)}</TableCell>
                    <TableCell align="right">{formatNumber(room.heatingLoad)}</TableCell>
                    <TableCell align="right">{formatNumber(room.coolingLoad)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </DataCard>
      ) : null}
    </Stack>
  );
}

interface MetricProps {
  label: string;
  value: number | string | null | undefined;
}

function Metric({ label, value }: MetricProps): JSX.Element {
  const displayValue = typeof value === "number" ? formatNumber(value) : value ?? "-";

  return (
    <Stack spacing={0.5} sx={{ minWidth: 150 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700 }}>
        {displayValue}
      </Typography>
    </Stack>
  );
}
