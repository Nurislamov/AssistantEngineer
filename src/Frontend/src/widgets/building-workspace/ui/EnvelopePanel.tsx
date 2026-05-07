import { Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { RoomDto } from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";
import { RoomSelect } from "./RoomSelect";
import { WallEditor } from "./WallEditor";
import { WindowEditor } from "./WindowEditor";

interface EnvelopePanelProps {
  rooms: RoomDto[];
}

export function EnvelopePanel({ rooms }: EnvelopePanelProps): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;

  const wallsQuery = useQuery({
    queryKey: queryKeys.rooms.walls(selectedRoomId),
    queryFn: () => roomsApi.getWalls(selectedRoomId),
    enabled: selectedRoomId > 0,
  });

  const windowsQuery = useQuery({
    queryKey: queryKeys.rooms.windows(selectedRoomId),
    queryFn: () => roomsApi.getWindows(selectedRoomId),
    enabled: selectedRoomId > 0,
  });

  return (
    <Stack spacing={2}>
      <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
      <WallEditor
        roomId={selectedRoomId}
        items={wallsQuery.data ?? []}
        onChanged={() => {
          void wallsQuery.refetch();
        }}
        error={wallsQuery.error}
      />
      <WindowEditor
        roomId={selectedRoomId}
        items={windowsQuery.data ?? []}
        onChanged={() => {
          void windowsQuery.refetch();
        }}
        error={windowsQuery.error}
      />
    </Stack>
  );
}
