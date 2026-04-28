import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
import type { RoomDto } from "@/entities/room/types";
import { formatNumber } from "@/shared/lib/format";
import { EmptyState } from "@/shared/ui/EmptyState";

interface RoomListProps {
  rooms: RoomDto[];
  renderActions?: (room: RoomDto) => JSX.Element;
}

export function RoomList({ rooms, renderActions }: RoomListProps): JSX.Element {
  if (rooms.length === 0) {
    return (
      <EmptyState
        title="Помещений пока нет"
        description="Добавьте этаж и создайте первое помещение здания."
      />
    );
  }

  return (
    <TableContainer>
      <Table size="medium">
        <TableHead>
          <TableRow>
            <TableCell>Помещение</TableCell>
            <TableCell>Этаж</TableCell>
            <TableCell align="right">Площадь, м²</TableCell>
            <TableCell align="right">Объём, м³</TableCell>
            <TableCell align="right">Tвн, °C</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rooms.map((room) => (
            <TableRow key={room.id} hover>
              <TableCell>{room.name}</TableCell>
              <TableCell>{room.floorName ?? room.floorId}</TableCell>
              <TableCell align="right">{formatNumber(room.area)}</TableCell>
              <TableCell align="right">{formatNumber(room.volume)}</TableCell>
              <TableCell align="right">{formatNumber(room.designIndoorTemperature)}</TableCell>
              <TableCell align="right">{renderActions?.(room)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
