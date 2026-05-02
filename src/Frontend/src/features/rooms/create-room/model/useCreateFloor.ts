import { useMutation, useQueryClient } from "@tanstack/react-query";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import type { CreateFloorRequest } from "@/entities/floor/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useCreateFloor(buildingId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateFloorRequest) => floorsApi.create(buildingId, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.floors.byBuilding(buildingId) });
      await queryClient.invalidateQueries({ queryKey: queryKeys.rooms.byBuilding(buildingId) });
    },
  });
}
