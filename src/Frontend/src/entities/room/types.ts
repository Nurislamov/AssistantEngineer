export type RoomTypeDto =
  | "Office"
  | "MeetingRoom"
  | "Corridor"
  | "ServerRoom"
  | "Retail"
  | "Residential"
  | "Other";

export type RoomTypeApiValue = RoomTypeDto | number;

export interface RoomDto {
  id: number;
  buildingId?: number;
  floorId: number;
  floorName?: string;
  name: string;
  floor?: number;
  area: number;
  height?: number;
  volume?: number;
  designIndoorTemperature?: number;
  outdoorTemperatureOverride?: number | null;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  type: RoomTypeDto;
}

export interface CreateRoomRequest {
  name: string;
  floorId: number;
  floor?: number;
  area: number;
  height?: number;
  volume?: number;
  designIndoorTemperature?: number;
  outdoorTemperatureOverride?: number | null;
  peopleCount?: number;
  equipmentLoadW?: number;
  lightingLoadW?: number;
  type?: RoomTypeDto;
}

export interface RoomApiResponse {
  id: number;
  name: string;
  areaM2: number;
  heightM: number;
  volumeM3: number;
  indoorTemperatureC: number;
  outdoorTemperatureOverrideC?: number | null;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  type: RoomTypeApiValue;
  floorId: number;
}

export interface CreateRoomApiRequest {
  name: string;
  areaM2: number;
  heightM: number;
  indoorTemperatureC: number;
  outdoorTemperatureOverrideC?: number | null;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  type: number;
  floorId: number;
}
