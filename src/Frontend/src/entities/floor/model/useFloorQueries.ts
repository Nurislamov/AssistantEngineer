import { useQuery } from "@tanstack/react-query";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import { queryKeys } from "@/shared/api/queryKeys";

export function useBuildingFloors(buildingId: number) {
  return useQuery({
    queryKey: queryKeys.floors.byBuilding(buildingId),
    queryFn: () => floorsApi.getByBuildingId(buildingId),
    enabled: Number.isFinite(buildingId) && buildingId > 0,
  });
}
