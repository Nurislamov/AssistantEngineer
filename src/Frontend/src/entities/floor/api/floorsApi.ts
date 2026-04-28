import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type { CreateFloorRequest, FloorApiResponse, FloorDto } from "../types";

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
};
