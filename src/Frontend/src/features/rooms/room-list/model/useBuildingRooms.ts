import { useQuery } from "@tanstack/react-query";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { RoomDto } from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";

async function getRoomsForBuilding(buildingId: number): Promise<RoomDto[]> {
  // Current API exposes rooms by floor, so the building-level list is composed here.
  const floors = await floorsApi.getByBuildingId(buildingId);
  const roomsByFloor = await Promise.all(floors.map((floor) => roomsApi.getByFloor(floor)));

  return roomsByFloor.flat();
}

export function useBuildingRooms(buildingId: number) {
  return useQuery({
    queryKey: queryKeys.rooms.byBuilding(buildingId),
    queryFn: () => getRoomsForBuilding(buildingId),
    enabled: Number.isFinite(buildingId) && buildingId > 0,
  });
}
