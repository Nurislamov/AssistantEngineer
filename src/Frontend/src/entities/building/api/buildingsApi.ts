import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type {
  BuildingApiResponse,
  BuildingDto,
  CreateBuildingRequest,
  CreateProjectRequest,
  UpdateBuildingRequest,
  UpdateProjectRequest,
  BuildingReadinessApiResponse,
  BuildingValidationApiResponse,
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

  async update(projectId: number, request: UpdateProjectRequest): Promise<ProjectDto> {
    const response = await apiRequest<ProjectApiResponse>(apiRoutes.projects.update(projectId), {
      method: "PUT",
      body: request,
    });

    return mapProject(response);
  },

  async delete(projectId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.projects.delete(projectId), {
      method: "DELETE",
    });
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

  async update(buildingId: number, request: UpdateBuildingRequest): Promise<BuildingDto> {
    const response = await apiRequest<BuildingApiResponse>(apiRoutes.buildings.update(buildingId), {
      method: "PUT",
      body: {
        name: request.name,
        climateZoneId: request.climateZoneId ?? null,
      },
    });

    return mapBuilding(response);
  },

  async delete(buildingId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.buildings.delete(buildingId), {
      method: "DELETE",
    });
  },

  async getReadiness(buildingId: number): Promise<BuildingReadinessApiResponse> {
    return apiRequest<BuildingReadinessApiResponse>(apiRoutes.buildings.readiness(buildingId));
  },

  async getValidation(buildingId: number): Promise<BuildingValidationApiResponse> {
    return apiRequest<BuildingValidationApiResponse>(apiRoutes.buildings.validation(buildingId));
  },
};
