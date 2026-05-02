import { useMutation, useQueryClient } from "@tanstack/react-query";
import { buildingsApi } from "@/entities/building/api/buildingsApi";
import type { CreateBuildingRequest } from "@/entities/building/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useCreateBuilding(projectId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateBuildingRequest) => buildingsApi.create(projectId, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.buildings.byProject(projectId) });
    },
  });
}
