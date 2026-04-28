import { useMutation, useQueryClient } from "@tanstack/react-query";
import { projectsApi } from "@/entities/building/api/buildingsApi";
import type { CreateProjectRequest } from "@/entities/building/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useCreateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateProjectRequest) => projectsApi.create(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.projects.all });
    },
  });
}
