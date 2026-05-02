import { useQuery } from "@tanstack/react-query";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { RoomDto } from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";

async function getRoomsForBuilding(buildingId: number): Promise<RoomDto[]> {
  const floors = await floorsApi.getByBuildingId(buildingId);
  return roomsApi.getByBuilding(buildingId, floors);
}

export function useBuildingRooms(buildingId: number) {
  return useQuery({
    queryKey: queryKeys.rooms.byBuilding(buildingId),
    queryFn: () => getRoomsForBuilding(buildingId),
    enabled: Number.isFinite(buildingId) && buildingId > 0,
  });
}
