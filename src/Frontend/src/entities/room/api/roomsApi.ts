import { apiRoutes } from "@/shared/api/apiRoutes";
import type { FloorDto } from "@/entities/floor/types";
import { ApiError } from "@/shared/api/httpClient";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import { toRoomTypeApiValue, toRoomTypeDto } from "../lib/roomTypeMapper";
import type {
  CreateRoomApiRequest,
  CreateRoomRequest,
  NaturalVentilationPreviewDto,
  NaturalVentilationPreviewRequest,
  RoomGroundContactDto,
  RoomApiResponse,
  RoomDto,
  RoomVentilationDefaultsDto,
  RoomVentilationParametersDto,
  ThermalZoneApiResponse,
  ThermalZoneDto,
  UpdateRoomApiRequest,
  UpdateRoomRequest,
  UpsertRoomGroundContactRequest,
  UpsertRoomVentilationParametersRequest,
  UpsertThermalZoneRequest,
  UpsertWallRequest,
  UpsertWindowRequest,
  WallApiResponse,
  WallDto,
  WindowApiResponse,
  WindowDto,
} from "../types";

function mapRoom(response: RoomApiResponse, floor?: FloorDto): RoomDto {
  return {
    id: response.id,
    buildingId: floor?.buildingId,
    floorId: response.floorId,
    floorName: floor?.name,
    name: response.name,
    area: response.areaM2,
    height: response.heightM,
    volume: response.volumeM3,
    designIndoorTemperature: response.indoorTemperatureC,
    outdoorTemperatureOverride: response.outdoorTemperatureOverrideC,
    peopleCount: response.peopleCount,
    equipmentLoadW: response.equipmentLoadW,
    lightingLoadW: response.lightingLoadW,
    type: toRoomTypeDto(response.type),
  };
}

function toApiRequest(request: CreateRoomRequest): CreateRoomApiRequest {
  return {
    name: request.name,
    floorId: request.floorId,
    areaM2: request.area,
    heightM: request.height ?? 3,
    indoorTemperatureC: request.designIndoorTemperature ?? 22,
    outdoorTemperatureOverrideC: request.outdoorTemperatureOverride ?? null,
    peopleCount: request.peopleCount ?? 0,
    equipmentLoadW: request.equipmentLoadW ?? 0,
    lightingLoadW: request.lightingLoadW ?? 0,
    type: toRoomTypeApiValue(request.type ?? "Office"),
  };
}

function toUpdateApiRequest(request: UpdateRoomRequest): UpdateRoomApiRequest {
  return {
    name: request.name,
    areaM2: request.area,
    heightM: request.height ?? 3,
    indoorTemperatureC: request.designIndoorTemperature ?? 22,
    outdoorTemperatureOverrideC: request.outdoorTemperatureOverride ?? null,
    peopleCount: request.peopleCount ?? 0,
    equipmentLoadW: request.equipmentLoadW ?? 0,
    lightingLoadW: request.lightingLoadW ?? 0,
    type: toRoomTypeApiValue(request.type ?? "Office"),
  };
}

export async function getRoomsByFloor(floor: FloorDto): Promise<RoomDto[]> {
  const response = await apiRequest<PagedResponse<RoomApiResponse>>(apiRoutes.rooms.list(), {
    query: { floorId: floor.id, pageSize: 500, sortBy: "id" },
  });

  return response.items.map((room) => mapRoom(room, floor));
}

export const roomsApi = {
  getByFloor: getRoomsByFloor,

  async getByBuilding(buildingId: number, floors: FloorDto[] = []): Promise<RoomDto[]> {
    const response = await apiRequest<PagedResponse<RoomApiResponse>>(
      apiRoutes.rooms.listByBuilding(buildingId),
      {
        query: { pageSize: 500, sortBy: "id" },
      },
    );
    const floorById = new Map(floors.map((floor) => [floor.id, floor]));
    return response.items.map((room) => mapRoom(room, floorById.get(room.floorId)));
  },

  async getById(roomId: number): Promise<RoomDto> {
    const response = await apiRequest<RoomApiResponse>(apiRoutes.rooms.byId(roomId));
    return mapRoom(response);
  },

  async create(request: CreateRoomRequest): Promise<RoomDto> {
    const response = await apiRequest<RoomApiResponse>(apiRoutes.rooms.create(), {
      method: "POST",
      body: toApiRequest(request),
    });

    return mapRoom(response);
  },

  async update(roomId: number, request: UpdateRoomRequest): Promise<RoomDto> {
    const response = await apiRequest<RoomApiResponse>(apiRoutes.rooms.update(roomId), {
      method: "PUT",
      body: toUpdateApiRequest(request),
    });

    return mapRoom(response);
  },

  async delete(roomId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.rooms.delete(roomId), {
      method: "DELETE",
    });
  },

  async getWalls(roomId: number): Promise<WallDto[]> {
    const response = await apiRequest<PagedResponse<WallApiResponse>>(apiRoutes.rooms.walls(roomId), {
      query: { pageSize: 500, sortBy: "id" },
    });

    return response.items;
  },

  async createWall(roomId: number, request: UpsertWallRequest): Promise<WallDto> {
    return apiRequest<WallApiResponse>(apiRoutes.rooms.walls(roomId), {
      method: "POST",
      body: request,
    });
  },

  async updateWall(roomId: number, wallId: number, request: UpsertWallRequest): Promise<WallDto> {
    return apiRequest<WallApiResponse>(apiRoutes.rooms.wall(roomId, wallId), {
      method: "PUT",
      body: request,
    });
  },

  async deleteWall(roomId: number, wallId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.rooms.wall(roomId, wallId), {
      method: "DELETE",
    });
  },

  async getWindows(roomId: number): Promise<WindowDto[]> {
    const response = await apiRequest<PagedResponse<WindowApiResponse>>(
      apiRoutes.rooms.windows(roomId),
      {
        query: { pageSize: 500, sortBy: "id" },
      },
    );

    return response.items;
  },

  async createWindow(roomId: number, request: UpsertWindowRequest): Promise<WindowDto> {
    return apiRequest<WindowApiResponse>(apiRoutes.rooms.windows(roomId), {
      method: "POST",
      body: request,
    });
  },

  async updateWindow(
    roomId: number,
    windowId: number,
    request: UpsertWindowRequest,
  ): Promise<WindowDto> {
    return apiRequest<WindowApiResponse>(apiRoutes.rooms.window(roomId, windowId), {
      method: "PUT",
      body: request,
    });
  },

  async deleteWindow(roomId: number, windowId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.rooms.window(roomId, windowId), {
      method: "DELETE",
    });
  },

  async getVentilation(roomId: number): Promise<RoomVentilationParametersDto | null> {
    try {
      return await apiRequest<RoomVentilationParametersDto>(apiRoutes.rooms.ventilation(roomId));
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return null;
      }

      throw error;
    }
  },

  async upsertVentilation(
    roomId: number,
    request: UpsertRoomVentilationParametersRequest,
  ): Promise<RoomVentilationParametersDto> {
    return apiRequest<RoomVentilationParametersDto>(apiRoutes.rooms.ventilation(roomId), {
      method: "PUT",
      body: request,
    });
  },

  async deleteVentilation(roomId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.rooms.ventilation(roomId), {
      method: "DELETE",
    });
  },

  async previewVentilationDefaults(roomId: number): Promise<RoomVentilationDefaultsDto> {
    return apiRequest<RoomVentilationDefaultsDto>(apiRoutes.rooms.ventilationDefaults(roomId));
  },

  async applyVentilationDefaults(roomId: number, overwriteExistingParameters: boolean) {
    return apiRequest<RoomVentilationParametersDto>(
      apiRoutes.rooms.applyVentilationDefaults(roomId),
      {
        method: "POST",
        body: { overwriteExistingParameters },
      },
    );
  },

  async previewNaturalVentilation(
    roomId: number,
    request: NaturalVentilationPreviewRequest,
  ): Promise<NaturalVentilationPreviewDto> {
    return apiRequest<NaturalVentilationPreviewDto>(
      apiRoutes.rooms.naturalVentilationPreview(roomId),
      {
        method: "POST",
        body: request,
      },
    );
  },

  async getGroundContact(roomId: number): Promise<RoomGroundContactDto | null> {
    try {
      return await apiRequest<RoomGroundContactDto>(apiRoutes.rooms.groundContact(roomId));
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return null;
      }

      throw error;
    }
  },

  async upsertGroundContact(
    roomId: number,
    request: UpsertRoomGroundContactRequest,
  ): Promise<RoomGroundContactDto> {
    return apiRequest<RoomGroundContactDto>(apiRoutes.rooms.groundContact(roomId), {
      method: "PUT",
      body: request,
    });
  },

  async deleteGroundContact(roomId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.rooms.groundContact(roomId), {
      method: "DELETE",
    });
  },
};

export const thermalZonesApi = {
  async getByBuilding(buildingId: number): Promise<ThermalZoneDto[]> {
    const response = await apiRequest<PagedResponse<ThermalZoneApiResponse>>(
      apiRoutes.thermalZones.listByBuilding(buildingId),
      {
        query: { pageSize: 500, sortBy: "id" },
      },
    );

    return response.items;
  },

  async create(buildingId: number, request: UpsertThermalZoneRequest): Promise<ThermalZoneDto> {
    return apiRequest<ThermalZoneApiResponse>(apiRoutes.thermalZones.create(buildingId), {
      method: "POST",
      body: request,
    });
  },

  async update(zoneId: number, request: UpsertThermalZoneRequest): Promise<ThermalZoneDto> {
    return apiRequest<ThermalZoneApiResponse>(apiRoutes.thermalZones.update(zoneId), {
      method: "PUT",
      body: request,
    });
  },

  async delete(zoneId: number): Promise<void> {
    await apiRequest<void>(apiRoutes.thermalZones.delete(zoneId), {
      method: "DELETE",
    });
  },
};
