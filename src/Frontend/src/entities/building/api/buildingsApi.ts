import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type {
  BuildingApiResponse,
  BuildingDto,
  CreateBuildingRequest,
  CreateProjectRequest,
  ProjectApiResponse,
  ProjectDto,
} from "../types";

function mapBuilding(response: BuildingApiResponse): BuildingDto {
  return {
    id: response.id,
    projectId: response.projectId,
    name: response.name,
    climateZoneId: response.climateZoneId,
    climateZoneName: response.climateZoneName,
  };
}

function mapProject(response: ProjectApiResponse): ProjectDto {
  return {
    id: response.id,
    name: response.name,
  };
}

export const projectsApi = {
  async getAll(): Promise<ProjectDto[]> {
    const response = await apiRequest<PagedResponse<ProjectApiResponse>>(apiRoutes.projects.list(), {
      query: { pageSize: 500, sortBy: "id" },
    });

    return response.items.map(mapProject);
  },

  async create(request: CreateProjectRequest): Promise<ProjectDto> {
    const response = await apiRequest<ProjectApiResponse>(apiRoutes.projects.create(), {
      method: "POST",
      body: request,
    });

    return mapProject(response);
  },
};

export const buildingsApi = {
  async getByProject(projectId: number): Promise<BuildingDto[]> {
    const response = await apiRequest<PagedResponse<BuildingApiResponse>>(
      apiRoutes.buildings.listByProject(projectId),
      {
        query: { pageSize: 500, sortBy: "id" },
      },
    );

    return response.items.map(mapBuilding);
  },

  async getById(buildingId: number): Promise<BuildingDto> {
    const response = await apiRequest<BuildingApiResponse>(apiRoutes.buildings.byId(buildingId));
    return mapBuilding(response);
  },

  async create(projectId: number, request: CreateBuildingRequest): Promise<BuildingDto> {
    const response = await apiRequest<BuildingApiResponse>(apiRoutes.buildings.create(projectId), {
      method: "POST",
      body: {
        name: request.name,
        climateZoneId: request.climateZoneId ?? null,
      },
    });

    return mapBuilding(response);
  },

  // TODO: add update/delete methods when backend exposes PUT/DELETE /api/v1/buildings/{buildingId}.
};
