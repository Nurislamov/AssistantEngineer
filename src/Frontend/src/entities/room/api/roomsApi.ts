import { apiRoutes } from "@/shared/api/apiRoutes";
import type { FloorDto } from "@/entities/floor/types";
import { apiRequest } from "@/shared/api/httpClient";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import { toRoomTypeApiValue, toRoomTypeDto } from "../lib/roomTypeMapper";
import type {
  CreateRoomApiRequest,
  CreateRoomRequest,
  RoomApiResponse,
  RoomDto,
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

export async function getRoomsByFloor(floor: FloorDto): Promise<RoomDto[]> {
  const response = await apiRequest<PagedResponse<RoomApiResponse>>(apiRoutes.rooms.list(), {
    query: { floorId: floor.id, pageSize: 500, sortBy: "id" },
  });

  return response.items.map((room) => mapRoom(room, floor));
}

export const roomsApi = {
  getByFloor: getRoomsByFloor,

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

  // TODO: add update/delete methods when backend exposes PUT/DELETE /api/v1/rooms/{roomId}.
};
