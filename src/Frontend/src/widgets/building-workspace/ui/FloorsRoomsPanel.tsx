import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditIcon from "@mui/icons-material/Edit";
import SaveIcon from "@mui/icons-material/Save";
import {
  Alert,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import type { FloorDto } from "@/entities/floor/types";
import type { RoomDto, RoomTypeDto } from "@/entities/room/types";
import { formatNumber } from "@/shared/lib/format";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import {
  toRoomForm,
  useFloorsRoomsMutations,
} from "../model/useFloorsRoomsMutations";

const roomTypes: RoomTypeDto[] = [
  "Office",
  "MeetingRoom",
  "Corridor",
  "ServerRoom",
  "Retail",
  "Residential",
  "Other",
];

interface FloorsRoomsPanelProps {
  buildingId: number;
  floors: FloorDto[];
  rooms: RoomDto[];
}

export function FloorsRoomsPanel({
  buildingId,
  floors,
  rooms,
}: FloorsRoomsPanelProps): JSX.Element {
  const {
    floorName,
    setFloorName,
    editingFloorId,
    setEditingFloorId,
    roomForm,
    setRoomForm,
    editingRoomId,
    setEditingRoomId,
    effectiveRoomFloorId,
    saveFloor,
    deleteFloor,
    saveRoom,
    deleteRoom,
    resetRoomForm,
  } = useFloorsRoomsMutations({
    buildingId,
    floors,
  });

  const beginFloorEdit = (floor: FloorDto) => {
    setEditingFloorId(floor.id);
    setFloorName(floor.name);
  };

  const beginRoomEdit = (room: RoomDto) => {
    setEditingRoomId(room.id);
    setRoomForm(toRoomForm(room));
  };

  return (
    <Stack spacing={2}>
      <DataCard>
        <Stack spacing={2}>
          <Typography variant="h6">Floors</Typography>
          {(saveFloor.isError || deleteFloor.isError) ? (
            <Alert severity="error">{getErrorMessage(saveFloor.error ?? deleteFloor.error)}</Alert>
          ) : null}
          <Stack component="form" direction={{ xs: "column", sm: "row" }} spacing={1} onSubmit={(event) => {
            event.preventDefault();
            saveFloor.mutate();
          }}>
            <TextField
              label="Floor name"
              value={floorName}
              required
              onChange={(event) => setFloorName(event.target.value)}
              size="small"
            />
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={saveFloor.isPending}>
              {editingFloorId ? "Save floor" : "Add floor"}
            </Button>
          </Stack>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {floors.map((floor) => (
                  <TableRow key={floor.id}>
                    <TableCell>{floor.id}</TableCell>
                    <TableCell>{floor.name}</TableCell>
                    <TableCell align="right">
                      <Button size="small" startIcon={<EditIcon />} onClick={() => beginFloorEdit(floor)}>
                        Edit
                      </Button>
                      <Button
                        size="small"
                        color="error"
                        startIcon={<DeleteOutlineIcon />}
                        onClick={() => {
                          if (window.confirm(`Delete floor "${floor.name}"?`)) deleteFloor.mutate(floor.id);
                        }}
                      >
                        Delete
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </DataCard>

      <DataCard>
        <Stack spacing={2}>
          <Typography variant="h6">Rooms</Typography>
          {floors.length === 0 ? <EmptyState title="No floors" description="Create a floor before adding rooms." /> : null}
          {(saveRoom.isError || deleteRoom.isError) ? (
            <Alert severity="error">{getErrorMessage(saveRoom.error ?? deleteRoom.error)}</Alert>
          ) : null}
          <Stack component="form" spacing={1.5} onSubmit={(event) => {
            event.preventDefault();
            saveRoom.mutate();
          }}>
            <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
              <TextField label="Room name" size="small" required value={roomForm.name} onChange={(event) => setRoomForm((current) => ({ ...current, name: event.target.value }))} />
              <FormControl size="small" sx={{ minWidth: 160 }}>
                <InputLabel>Floor</InputLabel>
                <Select label="Floor" value={effectiveRoomFloorId || ""} onChange={(event) => setRoomForm((current) => ({ ...current, floorId: Number(event.target.value) }))}>
                  {floors.map((floor) => <MenuItem key={floor.id} value={floor.id}>{floor.name}</MenuItem>)}
                </Select>
              </FormControl>
              <TextField label="Area m2" type="number" size="small" value={roomForm.area} onChange={(event) => setRoomForm((current) => ({ ...current, area: Number(event.target.value) }))} />
              <TextField label="Height m" type="number" size="small" value={roomForm.height ?? 3} onChange={(event) => setRoomForm((current) => ({ ...current, height: Number(event.target.value) }))} />
              <FormControl size="small" sx={{ minWidth: 140 }}>
                <InputLabel>Type</InputLabel>
                <Select label="Type" value={roomForm.type ?? "Office"} onChange={(event) => setRoomForm((current) => ({ ...current, type: event.target.value as RoomTypeDto }))}>
                  {roomTypes.map((type) => <MenuItem key={type} value={type}>{type}</MenuItem>)}
                </Select>
              </FormControl>
            </Stack>
            <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
              <TextField label="Indoor C" type="number" size="small" value={roomForm.designIndoorTemperature ?? 22} onChange={(event) => setRoomForm((current) => ({ ...current, designIndoorTemperature: Number(event.target.value) }))} />
              <TextField label="Outdoor override C" type="number" size="small" value={roomForm.outdoorTemperatureOverride ?? ""} onChange={(event) => setRoomForm((current) => ({ ...current, outdoorTemperatureOverride: event.target.value ? Number(event.target.value) : null }))} />
              <TextField label="People" type="number" size="small" value={roomForm.peopleCount ?? 0} onChange={(event) => setRoomForm((current) => ({ ...current, peopleCount: Number(event.target.value) }))} />
              <TextField label="Equipment W" type="number" size="small" value={roomForm.equipmentLoadW ?? 0} onChange={(event) => setRoomForm((current) => ({ ...current, equipmentLoadW: Number(event.target.value) }))} />
              <TextField label="Lighting W" type="number" size="small" value={roomForm.lightingLoadW ?? 0} onChange={(event) => setRoomForm((current) => ({ ...current, lightingLoadW: Number(event.target.value) }))} />
              <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={saveRoom.isPending || effectiveRoomFloorId <= 0}>
                {editingRoomId ? "Save room" : "Add room"}
              </Button>
            </Stack>
          </Stack>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Floor</TableCell>
                  <TableCell>Area</TableCell>
                  <TableCell>Load W</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rooms.map((room) => (
                  <TableRow key={room.id}>
                    <TableCell>{room.id}</TableCell>
                    <TableCell>{room.name}</TableCell>
                    <TableCell>{room.floorName ?? room.floorId}</TableCell>
                    <TableCell>{formatNumber(room.area)}</TableCell>
                    <TableCell>{formatNumber(room.equipmentLoadW + room.lightingLoadW, 0)}</TableCell>
                    <TableCell align="right">
                      <Button size="small" startIcon={<EditIcon />} onClick={() => beginRoomEdit(room)}>
                        Edit
                      </Button>
                      <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} onClick={() => {
                        if (window.confirm(`Delete room "${room.name}"?`)) deleteRoom.mutate(room.id);
                      }}>
                        Delete
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          {editingRoomId ? (
            <Button
              variant="text"
              onClick={() => {
                setEditingRoomId(null);
                resetRoomForm();
              }}
              sx={{ alignSelf: "flex-start" }}
            >
              Cancel room edit
            </Button>
          ) : null}
        </Stack>
      </DataCard>
    </Stack>
  );
}
