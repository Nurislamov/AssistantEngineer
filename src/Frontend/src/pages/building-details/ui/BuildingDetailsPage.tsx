import { Alert, Stack } from "@mui/material";
import { useState } from "react";
import { useParams } from "react-router-dom";
import { useBuilding } from "@/entities/building/model/useBuildingsQueries";
import { useCachedBuildingCalculation } from "@/entities/calculation/model/useCachedCalculation";
import { useBuildingFloors } from "@/entities/floor/model/useFloorQueries";
import { CreateFloorForm } from "@/features/rooms/create-room/ui/CreateFloorForm";
import { CreateRoomForm } from "@/features/rooms/create-room/ui/CreateRoomForm";
import { useBuildingRooms } from "@/features/rooms/room-list/model/useBuildingRooms";
import { parsePositiveIntParam } from "@/shared/lib/routeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { FormDialog } from "@/shared/ui/FormDialog";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";
import { QueryState } from "@/shared/ui/QueryState";
import { BuildingDetailsActions } from "@/widgets/building-details-actions/ui/BuildingDetailsActions";
import { BuildingRoomsPanel } from "@/widgets/building-rooms-panel/ui/BuildingRoomsPanel";
import { BuildingSummary } from "@/widgets/building-summary/ui/BuildingSummary";
import { CalculationSummary } from "@/widgets/calculation-summary/ui/CalculationSummary";

export function BuildingDetailsPage(): JSX.Element {
  const params = useParams();
  const buildingId = parsePositiveIntParam(params.buildingId);
  const [createRoomOpen, setCreateRoomOpen] = useState(false);
  const [createFloorOpen, setCreateFloorOpen] = useState(false);
  const [operationError, setOperationError] = useState<string | null>(null);

  const queryBuildingId = buildingId ?? 0;
  const buildingQuery = useBuilding(queryBuildingId);
  const floorsQuery = useBuildingFloors(queryBuildingId);
  const roomsQuery = useBuildingRooms(queryBuildingId);
  const cachedCalculation = useCachedBuildingCalculation(buildingId);

  if (!buildingId) {
    return (
      <PageContainer>
        <ErrorState message="Некорректный ID здания в маршруте" />
      </PageContainer>
    );
  }

  const isLoading = buildingQuery.isLoading || floorsQuery.isLoading || roomsQuery.isLoading;
  const queryError = buildingQuery.error ?? floorsQuery.error ?? roomsQuery.error;

  return (
    <PageContainer>
      <PageHeader
        title="Карточка здания"
        actions={
          <BuildingDetailsActions
            buildingId={buildingId}
            onAddFloor={() => setCreateFloorOpen(true)}
            onAddRoom={() => setCreateRoomOpen(true)}
            onOperationError={(message) => setOperationError(message)}
          />
        }
      />

      {operationError ? (
        <Alert severity="error" onClose={() => setOperationError(null)} sx={{ mb: 2 }}>
          {operationError}
        </Alert>
      ) : null}
      <QueryState
        isLoading={isLoading}
        error={queryError}
        onRetry={() => {
          void buildingQuery.refetch();
          void floorsQuery.refetch();
          void roomsQuery.refetch();
        }}
      />

      {buildingQuery.data ? (
        <Stack spacing={3}>
          <BuildingSummary building={buildingQuery.data} />
          <CalculationSummary result={cachedCalculation} />
          <BuildingRoomsPanel
            rooms={roomsQuery.data ?? []}
            onOperationError={(message) => setOperationError(message)}
          />
        </Stack>
      ) : null}

      <FormDialog open={createFloorOpen} title="Добавить этаж" onClose={() => setCreateFloorOpen(false)}>
        <CreateFloorForm
          buildingId={buildingId}
          onCreated={() => setCreateFloorOpen(false)}
          onCancel={() => setCreateFloorOpen(false)}
        />
      </FormDialog>

      <FormDialog
        open={createRoomOpen}
        title="Добавить помещение"
        maxWidth="md"
        onClose={() => setCreateRoomOpen(false)}
      >
        <CreateRoomForm
          buildingId={buildingId}
          floors={floorsQuery.data ?? []}
          onCreated={() => setCreateRoomOpen(false)}
          onCancel={() => setCreateRoomOpen(false)}
        />
      </FormDialog>
    </PageContainer>
  );
}
