import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditIcon from "@mui/icons-material/Edit";
import SaveIcon from "@mui/icons-material/Save";
import {
  Alert,
  Box,
  Button,
  Chip,
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
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { buildingsApi } from "@/entities/building/api/buildingsApi";
import { useBuilding } from "@/entities/building/model/useBuildingsQueries";
import { equipmentCatalogApi, equipmentSelectionApi } from "@/entities/equipment/api/equipmentApi";
import type {
  EquipmentCatalogItemDto,
  EquipmentSelectionResultDto,
  UpsertEquipmentCatalogItemRequest,
} from "@/entities/equipment/types";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import type { FloorDto } from "@/entities/floor/types";
import { roomsApi, thermalZonesApi } from "@/entities/room/api/roomsApi";
import type {
  CardinalDirectionDto,
  GroundContactTypeDto,
  NaturalVentilationPreviewDto,
  RoomDto,
  RoomGroundContactDto,
  RoomTypeDto,
  RoomVentilationParametersDto,
  ThermalZoneDto,
  UpsertRoomGroundContactRequest,
  UpsertRoomVentilationParametersRequest,
  UpsertThermalZoneRequest,
  UpsertWallRequest,
  UpsertWindowRequest,
  WallBoundaryTypeDto,
  WallDto,
  WindowDto,
} from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";
import { formatNumber } from "@/shared/lib/format";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { QueryState } from "@/shared/ui/QueryState";
import { useBuildingWorkspaceData } from "../model/useBuildingWorkspaceData";
import { BuildingWorkspaceTabs, type WorkspaceTab } from "./BuildingWorkspaceTabs";
import { CalculationsPanel } from "./CalculationsPanel";
import { JsonBlock } from "./JsonBlock";
import { ReportsPanel } from "./ReportsPanel";
import { RoomSelect } from "./RoomSelect";

export { ReportsPanel } from "./ReportsPanel";

const directions: CardinalDirectionDto[] = [
  "North",
  "NorthEast",
  "East",
  "SouthEast",
  "South",
  "SouthWest",
  "West",
  "NorthWest",
];

const wallBoundaryTypes: WallBoundaryTypeDto[] = [
  "External",
  "Ground",
  "Adiabatic",
  "AdjacentConditioned",
  "AdjacentUnconditioned",
];

const roomTypes: RoomTypeDto[] = [
  "Office",
  "MeetingRoom",
  "Corridor",
  "ServerRoom",
  "Retail",
  "Residential",
  "Other",
];

const groundContactTypes: GroundContactTypeDto[] = [
  "SlabOnGround",
  "BasementConditioned",
  "BasementUnconditioned",
  "CrawlSpace",
  "VentilatedCrawlSpace",
];

interface BuildingWorkspaceProps {
  buildingId: number;
}

type RoomForm = {
  name: string;
  floorId: number;
  area: number;
  height: number;
  designIndoorTemperature: number;
  outdoorTemperatureOverride: number | null;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  type: RoomTypeDto;
};

export function BuildingWorkspace({ buildingId }: BuildingWorkspaceProps): JSX.Element {
  const [tab, setTab] = useState<WorkspaceTab>("summary");
  const { buildingQuery, floorsQuery, roomsQuery, queryError } = useBuildingWorkspaceData(buildingId);

  return (
    <Stack spacing={2}>
      <QueryState
        isLoading={buildingQuery.isLoading || floorsQuery.isLoading || roomsQuery.isLoading}
        error={queryError}
        onRetry={() => {
          void buildingQuery.refetch();
          void floorsQuery.refetch();
          void roomsQuery.refetch();
        }}
      />

      {buildingQuery.data ? (
        <>
          <BuildingWorkspaceTabs tab={tab} onChange={setTab} />

          {tab === "summary" ? <SummaryPanel buildingId={buildingId} /> : null}
          {tab === "floors" ? (
            <FloorsRoomsPanel
              buildingId={buildingId}
              floors={floorsQuery.data ?? []}
              rooms={roomsQuery.data ?? []}
            />
          ) : null}
          {tab === "envelope" ? <EnvelopePanel rooms={roomsQuery.data ?? []} /> : null}
          {tab === "zones" ? (
            <ThermalZonesPanel buildingId={buildingId} rooms={roomsQuery.data ?? []} />
          ) : null}
          {tab === "ventilation" ? <VentilationPanel rooms={roomsQuery.data ?? []} /> : null}
          {tab === "ground" ? <GroundContactPanel rooms={roomsQuery.data ?? []} /> : null}
          {tab === "calculations" ? (
            <CalculationsPanel buildingId={buildingId} rooms={roomsQuery.data ?? []} />
          ) : null}
          {tab === "reports" ? <ReportsPanel buildingId={buildingId} /> : null}
          {tab === "equipment" ? <EquipmentPanel rooms={roomsQuery.data ?? []} /> : null}
        </>
      ) : null}
    </Stack>
  );
}

function SummaryPanel({ buildingId }: { buildingId: number }): JSX.Element {
  const buildingQuery = useBuilding(buildingId);
  const readiness = useQuery({
    queryKey: ["building-readiness", buildingId],
    queryFn: () => buildingsApi.getReadiness(buildingId),
    enabled: false,
  });
  const validation = useQuery({
    queryKey: ["building-validation", buildingId],
    queryFn: () => buildingsApi.getValidation(buildingId),
    enabled: false,
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <Box sx={{ flex: 1 }}>
            <Typography variant="h6">{buildingQuery.data?.name}</Typography>
            <Typography variant="body2" color="text.secondary">
              Building #{buildingId} · Project #{buildingQuery.data?.projectId}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Climate zone: {buildingQuery.data?.climateZoneName ?? buildingQuery.data?.climateZoneId ?? "-"}
            </Typography>
          </Box>
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" onClick={() => void readiness.refetch()}>
              Check readiness
            </Button>
            <Button variant="outlined" onClick={() => void validation.refetch()}>
              Validate
            </Button>
          </Stack>
        </Stack>
        {readiness.data ? <JsonBlock title="Readiness" value={readiness.data} /> : null}
        {validation.data ? <JsonBlock title="Validation" value={validation.data} /> : null}
        {readiness.error ? <Alert severity="error">{getErrorMessage(readiness.error)}</Alert> : null}
        {validation.error ? <Alert severity="error">{getErrorMessage(validation.error)}</Alert> : null}
      </Stack>
    </DataCard>
  );
}

function FloorsRoomsPanel({
  buildingId,
  floors,
  rooms,
}: {
  buildingId: number;
  floors: FloorDto[];
  rooms: RoomDto[];
}): JSX.Element {
  const queryClient = useQueryClient();
  const [floorName, setFloorName] = useState("");
  const [editingFloorId, setEditingFloorId] = useState<number | null>(null);
  const [roomForm, setRoomForm] = useState(() => createRoomForm(floors[0]?.id ?? 0));
  const [editingRoomId, setEditingRoomId] = useState<number | null>(null);
  const effectiveRoomFloorId = roomForm.floorId || floors[0]?.id || 0;

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.floors.byBuilding(buildingId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.rooms.byBuilding(buildingId) }),
    ]);
  };

  const saveFloor = useMutation({
    mutationFn: () =>
      editingFloorId
        ? floorsApi.update(editingFloorId, { name: floorName.trim() })
        : floorsApi.create(buildingId, { name: floorName.trim() }),
    onSuccess: async () => {
      setFloorName("");
      setEditingFloorId(null);
      await invalidate();
    },
  });
  const deleteFloor = useMutation({
    mutationFn: (floorId: number) => floorsApi.delete(floorId),
    onSuccess: invalidate,
  });
  const saveRoom = useMutation({
    mutationFn: () =>
      editingRoomId
        ? roomsApi.update(editingRoomId, roomForm)
        : roomsApi.create({
            ...roomForm,
            floorId: effectiveRoomFloorId,
          }),
    onSuccess: async () => {
      setRoomForm(createRoomForm(floors[0]?.id ?? 0));
      setEditingRoomId(null);
      await invalidate();
    },
  });
  const deleteRoom = useMutation({
    mutationFn: (roomId: number) => roomsApi.delete(roomId),
    onSuccess: invalidate,
  });

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
                      <Button size="small" startIcon={<EditIcon />} onClick={() => {
                        setEditingFloorId(floor.id);
                        setFloorName(floor.name);
                      }}>
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
                      <Button size="small" startIcon={<EditIcon />} onClick={() => {
                        setEditingRoomId(room.id);
                        setRoomForm({
                          name: room.name,
                          floorId: room.floorId,
                          area: room.area,
                          height: room.height ?? 3,
                          designIndoorTemperature: room.designIndoorTemperature ?? 22,
                          outdoorTemperatureOverride: room.outdoorTemperatureOverride ?? null,
                          peopleCount: room.peopleCount,
                          equipmentLoadW: room.equipmentLoadW,
                          lightingLoadW: room.lightingLoadW,
                          type: room.type,
                        });
                      }}>
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
        </Stack>
      </DataCard>
    </Stack>
  );
}

function EnvelopePanel({ rooms }: { rooms: RoomDto[] }): JSX.Element {
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
      <EnvelopeEditor
        title="Walls"
        type="wall"
        roomId={selectedRoomId}
        items={wallsQuery.data ?? []}
        refetch={() => {
          void wallsQuery.refetch();
        }}
        error={wallsQuery.error}
      />
      <EnvelopeEditor
        title="Windows"
        type="window"
        roomId={selectedRoomId}
        items={windowsQuery.data ?? []}
        refetch={() => {
          void windowsQuery.refetch();
        }}
        error={windowsQuery.error}
      />
    </Stack>
  );
}

function EnvelopeEditor({
  title,
  type,
  roomId,
  items,
  refetch,
  error,
}: {
  title: string;
  type: "wall" | "window";
  roomId: number;
  items: WallDto[] | WindowDto[];
  refetch: () => void;
  error: unknown;
}): JSX.Element {
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<UpsertWallRequest & Partial<UpsertWindowRequest>>(defaultEnvelopeForm);
  const save = useMutation<WallDto | WindowDto, Error, void>({
    mutationFn: () => {
      if (type === "wall") {
        const request: UpsertWallRequest = {
          areaM2: Number(form.areaM2),
          uValue: Number(form.uValue),
          orientation: form.orientation,
          boundaryType: form.boundaryType,
          adjacentRoomId: form.adjacentRoomId ?? null,
        };
        return editingId
          ? roomsApi.updateWall(roomId, editingId, request)
          : roomsApi.createWall(roomId, request);
      }

      const request: UpsertWindowRequest = {
        areaM2: Number(form.areaM2),
        uValue: Number(form.uValue),
        shgc: Number(form.shgc ?? 0.6),
        orientation: form.orientation,
        shading: form.shading ?? defaultWindowShading(),
      };
      return editingId
        ? roomsApi.updateWindow(roomId, editingId, request)
        : roomsApi.createWindow(roomId, request);
    },
    onSuccess: () => {
      setEditingId(null);
      setForm(defaultEnvelopeForm());
      refetch();
    },
  });
  const remove = useMutation({
    mutationFn: (id: number) =>
      type === "wall" ? roomsApi.deleteWall(roomId, id) : roomsApi.deleteWindow(roomId, id),
    onSuccess: refetch,
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">{title}</Typography>
        {(error || save.isError || remove.isError) ? (
          <Alert severity="error">{getErrorMessage(error ?? save.error ?? remove.error)}</Alert>
        ) : null}
        <Stack component="form" spacing={1.5} onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            <TextField label="Area m2" type="number" size="small" value={form.areaM2} onChange={(event) => setForm((current) => ({ ...current, areaM2: Number(event.target.value) }))} />
            <TextField label="U-value" type="number" size="small" value={form.uValue} onChange={(event) => setForm((current) => ({ ...current, uValue: Number(event.target.value) }))} />
            {type === "window" ? (
              <TextField label="SHGC" type="number" size="small" value={form.shgc ?? 0.6} onChange={(event) => setForm((current) => ({ ...current, shgc: Number(event.target.value) }))} />
            ) : null}
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Orientation</InputLabel>
              <Select label="Orientation" value={form.orientation} onChange={(event) => setForm((current) => ({ ...current, orientation: event.target.value as CardinalDirectionDto }))}>
                {directions.map((direction) => <MenuItem key={direction} value={direction}>{direction}</MenuItem>)}
              </Select>
            </FormControl>
            {type === "wall" ? (
              <FormControl size="small" sx={{ minWidth: 190 }}>
                <InputLabel>Boundary</InputLabel>
                <Select label="Boundary" value={form.boundaryType} onChange={(event) => setForm((current) => ({ ...current, boundaryType: event.target.value as WallBoundaryTypeDto }))}>
                  {wallBoundaryTypes.map((boundary) => <MenuItem key={boundary} value={boundary}>{boundary}</MenuItem>)}
                </Select>
              </FormControl>
            ) : null}
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={save.isPending || roomId <= 0}>
              {editingId ? "Save" : "Add"}
            </Button>
          </Stack>
        </Stack>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Area</TableCell>
                <TableCell>U-value</TableCell>
                <TableCell>Orientation</TableCell>
                <TableCell>{type === "wall" ? "Boundary" : "SHGC"}</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.id}</TableCell>
                  <TableCell>{formatNumber(item.areaM2)}</TableCell>
                  <TableCell>{formatNumber(item.uValue, 2)}</TableCell>
                  <TableCell>{item.orientation}</TableCell>
                  <TableCell>{"boundaryType" in item ? item.boundaryType : item.shgc}</TableCell>
                  <TableCell align="right">
                    <Button size="small" startIcon={<EditIcon />} onClick={() => {
                      setEditingId(item.id);
                      if ("boundaryType" in item) {
                        setForm({
                          areaM2: item.areaM2,
                          uValue: item.uValue,
                          orientation: item.orientation,
                          boundaryType: item.boundaryType,
                          adjacentRoomId: item.adjacentRoomId ?? null,
                        });
                      } else {
                        setForm({
                          ...defaultEnvelopeForm(),
                          areaM2: item.areaM2,
                          uValue: item.uValue,
                          orientation: item.orientation,
                          shgc: item.shgc,
                          shading: item.shading,
                        });
                      }
                    }}>
                      Edit
                    </Button>
                    <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} onClick={() => remove.mutate(item.id)}>
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
  );
}

function ThermalZonesPanel({ buildingId, rooms }: { buildingId: number; rooms: RoomDto[] }): JSX.Element {
  const query = useQuery({
    queryKey: queryKeys.thermalZones.byBuilding(buildingId),
    queryFn: () => thermalZonesApi.getByBuilding(buildingId),
  });
  const [editingId, setEditingId] = useState<number | null>(null);
  const [name, setName] = useState("");
  const [roomIds, setRoomIds] = useState<number[]>([]);
  const save = useMutation({
    mutationFn: () => {
      const request: UpsertThermalZoneRequest = { name: name.trim(), roomIds };
      return editingId
        ? thermalZonesApi.update(editingId, request)
        : thermalZonesApi.create(buildingId, request);
    },
    onSuccess: () => {
      setEditingId(null);
      setName("");
      setRoomIds([]);
      void query.refetch();
    },
  });
  const remove = useMutation({
    mutationFn: (zoneId: number) => thermalZonesApi.delete(zoneId),
    onSuccess: () => void query.refetch(),
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Thermal zones</Typography>
        {(query.error || save.isError || remove.isError) ? (
          <Alert severity="error">{getErrorMessage(query.error ?? save.error ?? remove.error)}</Alert>
        ) : null}
        <Stack component="form" direction={{ xs: "column", md: "row" }} spacing={1} onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}>
          <TextField label="Zone name" size="small" required value={name} onChange={(event) => setName(event.target.value)} />
          <FormControl size="small" sx={{ minWidth: 260 }}>
            <InputLabel>Rooms</InputLabel>
            <Select
              multiple
              label="Rooms"
              value={roomIds}
              onChange={(event) => setRoomIds((event.target.value as number[]) ?? [])}
              renderValue={(selected) => selected.join(", ")}
            >
              {rooms.map((room) => <MenuItem key={room.id} value={room.id}>{room.name}</MenuItem>)}
            </Select>
          </FormControl>
          <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={save.isPending}>
            {editingId ? "Save zone" : "Add zone"}
          </Button>
        </Stack>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Rooms</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(query.data ?? []).map((zone: ThermalZoneDto) => (
                <TableRow key={zone.id}>
                  <TableCell>{zone.id}</TableCell>
                  <TableCell>{zone.name}</TableCell>
                  <TableCell>{zone.rooms.map((room) => room.name).join(", ") || "-"}</TableCell>
                  <TableCell align="right">
                    <Button size="small" startIcon={<EditIcon />} onClick={() => {
                      setEditingId(zone.id);
                      setName(zone.name);
                      setRoomIds(zone.rooms.map((room) => room.id));
                    }}>
                      Edit
                    </Button>
                    <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} onClick={() => remove.mutate(zone.id)}>
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
  );
}

function VentilationPanel({ rooms }: { rooms: RoomDto[] }): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;
  const [form, setForm] = useState<UpsertRoomVentilationParametersRequest>(defaultVentilation());
  const [preview, setPreview] = useState<NaturalVentilationPreviewDto | RoomVentilationParametersDto | null>(null);
  const [operationError, setOperationError] = useState<string | null>(null);
  const query = useQuery({
    queryKey: queryKeys.rooms.ventilation(selectedRoomId),
    queryFn: () => roomsApi.getVentilation(selectedRoomId),
    enabled: selectedRoomId > 0,
    retry: false,
  });
  const save = useMutation({
    mutationFn: () => roomsApi.upsertVentilation(selectedRoomId, form),
    onSuccess: (data) => {
      setForm(data);
      setOperationError(null);
      void query.refetch();
    },
  });
  const remove = useMutation({
    mutationFn: () => roomsApi.deleteVentilation(selectedRoomId),
    onSuccess: () => {
      setOperationError(null);
      void query.refetch();
    },
  });

  const runVentilationOperation = async (
    operation: () => Promise<NaturalVentilationPreviewDto | RoomVentilationParametersDto>,
    onSuccess: (result: NaturalVentilationPreviewDto | RoomVentilationParametersDto) => void,
  ) => {
    setOperationError(null);
    try {
      onSuccess(await operation());
    } catch (error) {
      setOperationError(getErrorMessage(error));
    }
  };

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Ventilation</Typography>
        <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
        {(query.error || save.isError || remove.isError || operationError) ? (
          <Alert severity="warning">{operationError ?? getErrorMessage(query.error ?? save.error ?? remove.error)}</Alert>
        ) : null}
        {query.data ? <JsonBlock title="Current parameters" value={query.data} /> : <EmptyState title="No ventilation parameters" description="Save parameters or apply defaults for the selected room." />}
        <Stack component="form" spacing={1} onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            {Object.keys(defaultVentilation()).map((key) => (
              <TextField
                key={key}
                label={key}
                type="number"
                size="small"
                value={form[key as keyof UpsertRoomVentilationParametersRequest]}
                onChange={(event) => setForm((current) => ({ ...current, [key]: Number(event.target.value) }))}
              />
            ))}
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={save.isPending || selectedRoomId <= 0}>Save</Button>
            <Button variant="outlined" disabled={selectedRoomId <= 0} onClick={() => void runVentilationOperation(
              () => roomsApi.previewVentilationDefaults(selectedRoomId),
              setPreview,
            )}>Preview defaults</Button>
            <Button variant="outlined" disabled={selectedRoomId <= 0} onClick={() => void runVentilationOperation(
              () => roomsApi.applyVentilationDefaults(selectedRoomId, true),
              (result) => {
                setForm(result as RoomVentilationParametersDto);
                void query.refetch();
              },
            )}>Apply defaults</Button>
            <Button variant="outlined" disabled={selectedRoomId <= 0} onClick={() => void runVentilationOperation(
              () => roomsApi.previewNaturalVentilation(selectedRoomId, {
                indoorTemperatureC: 24,
                outdoorTemperatureC: 18,
                windSpeedMPerS: 2,
                demandFactor: 0.8,
                hourOfDay: 14,
              }),
              setPreview,
            )}>Natural preview</Button>
            <Button color="error" variant="outlined" disabled={remove.isPending || selectedRoomId <= 0} onClick={() => remove.mutate()}>Delete</Button>
          </Stack>
        </Stack>
        {preview ? <JsonBlock title="Preview" value={preview} /> : null}
      </Stack>
    </DataCard>
  );
}

function GroundContactPanel({ rooms }: { rooms: RoomDto[] }): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;
  const [form, setForm] = useState<UpsertRoomGroundContactRequest>(defaultGroundContact());
  const query = useQuery({
    queryKey: queryKeys.rooms.groundContact(selectedRoomId),
    queryFn: () => roomsApi.getGroundContact(selectedRoomId),
    enabled: selectedRoomId > 0,
    retry: false,
  });
  const save = useMutation({
    mutationFn: () => roomsApi.upsertGroundContact(selectedRoomId, form),
    onSuccess: (data) => {
      setForm(data);
      void query.refetch();
    },
  });
  const remove = useMutation({
    mutationFn: () => roomsApi.deleteGroundContact(selectedRoomId),
    onSuccess: () => void query.refetch(),
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Ground contact</Typography>
        <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
        {(query.error || save.isError || remove.isError) ? (
          <Alert severity="warning">{getErrorMessage(query.error ?? save.error ?? remove.error)}</Alert>
        ) : null}
        {query.data ? <JsonBlock title="Current ground contact" value={query.data} /> : <EmptyState title="No ground contact metadata" description="Save metadata for slab, basement, or crawl-space heat transfer." />}
        <Stack component="form" spacing={1} onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            <FormControl size="small" sx={{ minWidth: 190 }}>
              <InputLabel>Contact type</InputLabel>
              <Select label="Contact type" value={form.contactType} onChange={(event) => setForm((current) => ({ ...current, contactType: event.target.value as GroundContactTypeDto }))}>
                {groundContactTypes.map((type) => <MenuItem key={type} value={type}>{type}</MenuItem>)}
              </Select>
            </FormControl>
            {Object.keys(defaultGroundContact()).filter((key) => key !== "contactType").map((key) => (
              <TextField
                key={key}
                label={key}
                type="number"
                size="small"
                value={form[key as keyof RoomGroundContactDto]}
                onChange={(event) => setForm((current) => ({ ...current, [key]: Number(event.target.value) }))}
              />
            ))}
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={save.isPending || selectedRoomId <= 0}>Save</Button>
            <Button color="error" variant="outlined" disabled={remove.isPending || selectedRoomId <= 0} onClick={() => remove.mutate()}>Delete</Button>
          </Stack>
        </Stack>
      </Stack>
    </DataCard>
  );
}

export function EquipmentPanel({ rooms }: { rooms: RoomDto[] }): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;
  const query = useQuery({
    queryKey: queryKeys.equipmentCatalog.all,
    queryFn: equipmentCatalogApi.getAll,
  });
  const [form, setForm] = useState<UpsertEquipmentCatalogItemRequest>(defaultEquipment());
  const [editingId, setEditingId] = useState<number | null>(null);
  const [selection, setSelection] = useState<EquipmentSelectionResultDto | null>(null);
  const save = useMutation({
    mutationFn: () => editingId ? equipmentCatalogApi.update(editingId, form) : equipmentCatalogApi.create(form),
    onSuccess: () => {
      setEditingId(null);
      setForm(defaultEquipment());
      void query.refetch();
    },
  });
  const deactivate = useMutation({
    mutationFn: (id: number) => equipmentCatalogApi.deactivate(id),
    onSuccess: () => void query.refetch(),
  });
  const selectEquipment = useMutation({
    mutationFn: () => equipmentSelectionApi.selectForRoom(selectedRoomId, {
      systemType: form.systemType,
      unitType: form.unitType,
    }),
    onSuccess: setSelection,
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Equipment</Typography>
        {(query.error || save.isError || deactivate.isError || selectEquipment.isError) ? (
          <Alert severity="error">{getErrorMessage(query.error ?? save.error ?? deactivate.error ?? selectEquipment.error)}</Alert>
        ) : null}
        <Stack component="form" spacing={1} onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            <TextField label="Manufacturer" size="small" value={form.manufacturer} onChange={(event) => setForm((current) => ({ ...current, manufacturer: event.target.value }))} />
            <TextField label="System" size="small" value={form.systemType} onChange={(event) => setForm((current) => ({ ...current, systemType: event.target.value }))} />
            <TextField label="Unit" size="small" value={form.unitType} onChange={(event) => setForm((current) => ({ ...current, unitType: event.target.value }))} />
            <TextField label="Model" size="small" value={form.modelName} onChange={(event) => setForm((current) => ({ ...current, modelName: event.target.value }))} />
            <TextField label="Capacity kW" type="number" size="small" value={form.nominalCoolingCapacityKw} onChange={(event) => setForm((current) => ({ ...current, nominalCoolingCapacityKw: Number(event.target.value) }))} />
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={save.isPending}>{editingId ? "Save" : "Add"}</Button>
          </Stack>
        </Stack>
        <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
        <Button variant="outlined" disabled={selectEquipment.isPending || selectedRoomId <= 0} onClick={() => selectEquipment.mutate()}>
          Select equipment for room
        </Button>
        {selection ? <EquipmentSelectionSummary selection={selection} /> : <EmptyState title="No equipment selection" description="Select a room and run equipment selection after adding catalog items." />}
        <EquipmentCatalogTable
          items={query.data ?? []}
          onEdit={(item) => {
            setEditingId(item.id);
            setForm({
              manufacturer: item.manufacturer,
              systemType: item.systemType,
              unitType: item.unitType,
              modelName: item.modelName,
              nominalCoolingCapacityKw: item.nominalCoolingCapacityKw,
              isActive: item.isActive,
            });
          }}
          onDeactivate={(item) => deactivate.mutate(item.id)}
        />
      </Stack>
    </DataCard>
  );
}

function EquipmentCatalogTable({
  items,
  onEdit,
  onDeactivate,
}: {
  items: EquipmentCatalogItemDto[];
  onEdit: (item: EquipmentCatalogItemDto) => void;
  onDeactivate: (item: EquipmentCatalogItemDto) => void;
}): JSX.Element {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Model</TableCell>
            <TableCell>System</TableCell>
            <TableCell>Capacity</TableCell>
            <TableCell>Status</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item) => (
            <TableRow key={item.id}>
              <TableCell>{item.manufacturer} {item.modelName}</TableCell>
              <TableCell>{item.systemType} / {item.unitType}</TableCell>
              <TableCell>{formatNumber(item.nominalCoolingCapacityKw, 1)} kW</TableCell>
              <TableCell><Chip size="small" label={item.isActive ? "Active" : "Inactive"} color={item.isActive ? "success" : "default"} /></TableCell>
              <TableCell align="right">
                <Button size="small" startIcon={<EditIcon />} onClick={() => onEdit(item)}>Edit</Button>
                <Button size="small" color="error" startIcon={<DeleteOutlineIcon />} onClick={() => onDeactivate(item)}>Deactivate</Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

function EquipmentSelectionSummary({
  selection,
}: {
  selection: EquipmentSelectionResultDto;
}): JSX.Element {
  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">Recommended equipment</Typography>
      <TableContainer>
        <Table size="small">
          <TableBody>
            <TableRow>
              <TableCell>Selected model</TableCell>
              <TableCell>
                {selection.selectedManufacturer} {selection.selectedModelName}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Requested type</TableCell>
              <TableCell>
                {selection.requestedSystemType} / {selection.requestedUnitType}
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Design capacity</TableCell>
              <TableCell>{formatNumber(selection.designCapacityKw, 2)} kW</TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Selected capacity</TableCell>
              <TableCell>{formatNumber(selection.selectedNominalCoolingCapacityKw, 2)} kW</TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Reserve</TableCell>
              <TableCell>{formatNumber(selection.capacityReserveKw, 2)} kW</TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

function createRoomForm(floorId: number): RoomForm {
  return {
    name: "",
    floorId,
    area: 20,
    height: 3,
    designIndoorTemperature: 22,
    outdoorTemperatureOverride: null,
    peopleCount: 1,
    equipmentLoadW: 200,
    lightingLoadW: 100,
    type: "Office" as RoomTypeDto,
  };
}

function defaultWindowShading() {
  return {
    overhangDepthM: 0,
    sideFinDepthM: 0,
    revealDepthM: 0,
    windowHeightM: 0,
    windowWidthM: 0,
    minimumDirectSolarReductionFactor: 0.15,
    diffuseSolarShareUnaffected: 0.3,
  };
}

function defaultEnvelopeForm(): UpsertWallRequest & Partial<UpsertWindowRequest> {
  return {
    areaM2: 10,
    uValue: 1.2,
    orientation: "South",
    boundaryType: "External",
    adjacentRoomId: null,
    shgc: 0.55,
    shading: defaultWindowShading(),
  };
}

function defaultVentilation(): UpsertRoomVentilationParametersRequest {
  return {
    airChangesPerHour: 1,
    heatRecoveryEfficiency: 0,
    infiltrationAirChangesPerHour: 0.2,
    windExposureFactor: 1,
    stackCoefficient: 0.04,
    windCoefficient: 0.12,
  };
}

function defaultGroundContact(): UpsertRoomGroundContactRequest {
  return {
    contactType: "SlabOnGround",
    exposedPerimeterM: 10,
    burialDepthM: 0,
    wallHeightBelowGradeM: 0,
    horizontalInsulationWidthM: 0,
    perimeterInsulationDepthM: 0,
    underfloorVentilationAirChangesPerHour: 0,
  };
}

function defaultEquipment(): UpsertEquipmentCatalogItemRequest {
  return {
    manufacturer: "Demo HVAC",
    systemType: "Split",
    unitType: "WallMounted",
    modelName: "",
    nominalCoolingCapacityKw: 3.5,
    isActive: true,
  };
}



