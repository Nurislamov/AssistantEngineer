import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import { Alert, Button, Stack, Typography } from "@mui/material";
import { useState } from "react";
import type { RoomDto } from "@/entities/room/types";
import { calculationMethods } from "@/shared/constants/calculationMethods";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { useBuildingCalculationExecution } from "../model/useBuildingCalculationExecution";
import { JsonBlock } from "./JsonBlock";
import { RoomSelect } from "./RoomSelect";

export function CalculationsPanel({ buildingId, rooms }: { buildingId: number; rooms: RoomDto[] }): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;
  const [result, setResult] = useState<unknown>(null);

  const run = useBuildingCalculationExecution(buildingId, selectedRoomId);

  const runCalculation = (target: "building-loads" | "building-balance" | "room-loads") => {
    run.mutate(target, { onSuccess: setResult });
  };

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Calculations</Typography>
        {run.isError ? <Alert severity="error">{getErrorMessage(run.error)}</Alert> : null}
        <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          <Button variant="contained" startIcon={<PlayArrowIcon />} disabled={run.isPending} onClick={() => runCalculation("building-loads")}>Building heating/cooling</Button>
          <Button variant="outlined" startIcon={<PlayArrowIcon />} disabled={run.isPending} onClick={() => runCalculation("building-balance")}>Energy balance</Button>
          <Button variant="outlined" startIcon={<PlayArrowIcon />} disabled={run.isPending || selectedRoomId <= 0} onClick={() => runCalculation("room-loads")}>Room heating/cooling</Button>
        </Stack>
        <Typography variant="body2" color="text.secondary">
          Cooling method: {calculationMethods.cooling}; heating method: {calculationMethods.heating}.
        </Typography>
        {result ? <JsonBlock title="Latest calculation result" value={result} /> : <EmptyState title="No calculation result" description="Run a calculation. Results can be run again after refresh." />}
      </Stack>
    </DataCard>
  );
}
