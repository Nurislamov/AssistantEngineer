import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type { CreateFloorRequest, FloorApiResponse, FloorDto, UpdateFloorRequest } from "../types";

function mapFloor(response: FloorApiResponse): FloorDto {
  return {
    id: response.id,
    buildingId: response.buildingId,
    name: response.name,
  };
}

export const floorsApi = {
  async getByBuildingId(buildingId: number): Promise<FloorDto[]> {
    const response = await apiRequest<PagedResponse<FloorApiResponse>>(
      apiRoutes.floors.listByBuilding(buildingId),
      {
        query: { pageSize: 500, sortBy: "id" },
      },
    );

    return response.items.map(mapFloor);
  },

  async create(buildingId: number, request: CreateFloorRequest): Promise<FloorDto> {
    const response = await apiRequest<FloorApiResponse>(apiRoutes.floors.create(buildingId), {
      method: "POST",
      body: request,
    });

    return mapFloor(response);
  },

  async update(floorId: number, request: UpdateFloorRequest): Promise<FloorDto> {
    const response = await apiRequest<FloorApiResponse>(apiRoutes.floors.update(floorId), {
      method: "PUT",
      body: request,
    });

    return mapFloor(response);
  },

  async delete(floorId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.floors.delete(floorId), {
      method: "DELETE",
    });
  },
};
