export interface FloorDto {
  id: number;
  buildingId: number;
  name: string;
}

export interface CreateFloorRequest {
  name: string;
}

export interface FloorApiResponse {
  id: number;
  buildingId: number;
  name: string;
}
