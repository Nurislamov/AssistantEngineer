import { Stack, Typography } from "@mui/material";
import type { RoomDto } from "@/entities/room/types";
import { RunRoomCalculationButton } from "@/features/calculations/run-room-calculation/ui/RunRoomCalculationButton";
import { RoomList } from "@/features/rooms/room-list/ui/RoomList";
import { DataCard } from "@/shared/ui/DataCard";

interface BuildingRoomsPanelProps {
  rooms: RoomDto[];
  onOperationError: (message: string) => void;
}

export function BuildingRoomsPanel({
  rooms,
  onOperationError,
}: BuildingRoomsPanelProps): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          Помещения
        </Typography>
        <RoomList
          rooms={rooms}
          renderActions={(room) => (
            <RunRoomCalculationButton roomId={room.id} onError={onOperationError} />
          )}
        />
      </Stack>
    </DataCard>
  );
}
