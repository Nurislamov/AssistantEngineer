import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type {
  EquipmentCatalogItemApiResponse,
  EquipmentCatalogItemDto,
  EquipmentSelectionRequest,
  EquipmentSelectionResultDto,
  UpsertEquipmentCatalogItemRequest,
} from "../types";

export const equipmentCatalogApi = {
  async getAll(): Promise<EquipmentCatalogItemDto[]> {
    const response = await apiRequest<PagedResponse<EquipmentCatalogItemApiResponse>>(
      apiRoutes.equipmentCatalog.list(),
      {
        query: { pageSize: 500, sortBy: "id" },
      },
    );

    return response.items;
  },

  async create(request: UpsertEquipmentCatalogItemRequest): Promise<EquipmentCatalogItemDto> {
    return apiRequest<EquipmentCatalogItemApiResponse>(apiRoutes.equipmentCatalog.create(), {
      method: "POST",
      body: request,
    });
  },

  async update(
    id: number,
    request: UpsertEquipmentCatalogItemRequest,
  ): Promise<EquipmentCatalogItemDto> {
    return apiRequest<EquipmentCatalogItemApiResponse>(apiRoutes.equipmentCatalog.update(id), {
      method: "PUT",
      body: request,
    });
  },

  async deactivate(id: number): Promise<void> {
    await apiRequest<void>(apiRoutes.equipmentCatalog.delete(id), {
      method: "DELETE",
    });
  },
};

export const equipmentSelectionApi = {
  async selectForRoom(
    roomId: number,
    request: EquipmentSelectionRequest,
  ): Promise<EquipmentSelectionResultDto> {
    return apiRequest<EquipmentSelectionResultDto>(apiRoutes.rooms.equipmentSelection(roomId), {
      method: "POST",
      query: { method: "Iso52016" },
      body: request,
    });
  },
};
