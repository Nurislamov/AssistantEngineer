import { useBuilding } from "@/entities/building/model/useBuildingsQueries";
import { useBuildingFloors } from "@/entities/floor/model/useFloorQueries";
import { useBuildingRooms } from "@/features/rooms/room-list/model/useBuildingRooms";

export function useBuildingWorkspaceData(buildingId: number) {
  const buildingQuery = useBuilding(buildingId);
  const floorsQuery = useBuildingFloors(buildingId);
  const roomsQuery = useBuildingRooms(buildingId);
  const queryError = buildingQuery.error ?? floorsQuery.error ?? roomsQuery.error;

  return {
    buildingQuery,
    floorsQuery,
    roomsQuery,
    queryError,
  };
}
