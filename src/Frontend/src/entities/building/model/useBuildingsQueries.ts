import { useQuery } from "@tanstack/react-query";
import { buildingsApi, projectsApi } from "@/entities/building/api/buildingsApi";
import { queryKeys } from "@/shared/api/queryKeys";

export function useProjects() {
  return useQuery({
    queryKey: queryKeys.projects.all,
    queryFn: () => projectsApi.getAll(),
  });
}

export function useBuildings(projectId: number) {
  return useQuery({
    queryKey: queryKeys.buildings.byProject(projectId),
    queryFn: () => buildingsApi.getByProject(projectId),
    enabled: Number.isFinite(projectId) && projectId > 0,
  });
}

export function useBuilding(buildingId: number) {
  return useQuery({
    queryKey: queryKeys.buildings.detail(buildingId),
    queryFn: () => buildingsApi.getById(buildingId),
    enabled: Number.isFinite(buildingId) && buildingId > 0,
  });
}
