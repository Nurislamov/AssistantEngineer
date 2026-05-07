import { FormControl, InputLabel, MenuItem, Select } from "@mui/material";
import type { RoomDto } from "@/entities/room/types";

export function RoomSelect({
  rooms,
  roomId,
  onChange,
}: {
  rooms: RoomDto[];
  roomId: number;
  onChange: (roomId: number) => void;
}): JSX.Element {
  return (
    <FormControl size="small" sx={{ minWidth: 260, maxWidth: 360 }}>
      <InputLabel>Room</InputLabel>
      <Select label="Room" value={roomId || ""} onChange={(event) => onChange(Number(event.target.value))}>
        {rooms.map((room) => <MenuItem key={room.id} value={room.id}>{room.name}</MenuItem>)}
      </Select>
    </FormControl>
  );
}
