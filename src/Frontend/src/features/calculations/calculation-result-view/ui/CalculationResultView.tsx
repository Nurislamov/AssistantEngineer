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
        title="No calculation result"
        description="Run a calculation from the building workspace. After a page refresh, run it again from the Calculations tab."
      />
    );
  }

  return (
    <Stack spacing={3}>
      <DataCard>
        <Stack spacing={2}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {result.buildingName ?? result.roomName ?? "Calculation result"}
          </Typography>
          <Stack direction={{ xs: "column", md: "row" }} spacing={3}>
            <Metric label="Heat loss, W" value={result.totalHeatLoss} />
            <Metric label="Heat gain, W" value={result.totalHeatGain} />
            <Metric label="Heating load, W" value={result.heatingLoad} />
            <Metric label="Cooling load, W" value={result.coolingLoad} />
            <Metric label="Calculated at" value={formatDateTime(result.calculatedAt)} />
          </Stack>
        </Stack>
      </DataCard>

      {result.rooms && result.rooms.length > 0 ? (
        <DataCard>
          <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>
            Rooms
          </Typography>
          <TableContainer>
            <Table size="medium">
              <TableHead>
                <TableRow>
                  <TableCell>Room</TableCell>
                  <TableCell align="right">Heat loss, W</TableCell>
                  <TableCell align="right">Heat gain, W</TableCell>
                  <TableCell align="right">Heating, W</TableCell>
                  <TableCell align="right">Cooling, W</TableCell>
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
