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
import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { buildingsApi } from "@/entities/building/api/buildingsApi";
import { useBuilding } from "@/entities/building/model/useBuildingsQueries";
import { equipmentCatalogApi, equipmentSelectionApi } from "@/entities/equipment/api/equipmentApi";
import type {
  EquipmentCatalogItemDto,
  EquipmentSelectionResultDto,
  UpsertEquipmentCatalogItemRequest,
} from "@/entities/equipment/types";
import { roomsApi, thermalZonesApi } from "@/entities/room/api/roomsApi";
import type {
  RoomDto,
  ThermalZoneDto,
  UpsertThermalZoneRequest,
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
import { EnvelopePanel } from "./EnvelopePanel";
import { FloorsRoomsPanel } from "./FloorsRoomsPanel";
import { GroundContactPanel } from "./GroundContactPanel";
import { JsonBlock } from "./JsonBlock";
import { ReportsPanel } from "./ReportsPanel";
import { RoomSelect } from "./RoomSelect";
import { VentilationPanel } from "./VentilationPanel";

export { ReportsPanel } from "./ReportsPanel";

interface BuildingWorkspaceProps {
  buildingId: number;
}

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



