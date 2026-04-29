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

export type UpdateRoomRequest = Omit<CreateRoomRequest, "floorId" | "floor">;

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

export interface UpdateRoomApiRequest {
  name: string;
  areaM2: number;
  heightM: number;
  indoorTemperatureC: number;
  outdoorTemperatureOverrideC?: number | null;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  type: number;
}

export type CardinalDirectionDto =
  | "North"
  | "NorthEast"
  | "East"
  | "SouthEast"
  | "South"
  | "SouthWest"
  | "West"
  | "NorthWest";

export type WallBoundaryTypeDto =
  | "External"
  | "Ground"
  | "Adiabatic"
  | "AdjacentConditioned"
  | "AdjacentUnconditioned";

export interface WallDto {
  id: number;
  roomId: number;
  areaM2: number;
  uValue: number;
  orientation: CardinalDirectionDto;
  boundaryType: WallBoundaryTypeDto;
  isExternal: boolean;
  adjacentRoomId?: number | null;
}

export interface WallApiResponse extends WallDto {}

export interface UpsertWallRequest {
  areaM2: number;
  uValue: number;
  orientation: CardinalDirectionDto;
  boundaryType: WallBoundaryTypeDto;
  adjacentRoomId?: number | null;
}

export interface WindowShadingRequest {
  overhangDepthM: number;
  sideFinDepthM: number;
  revealDepthM: number;
  windowHeightM: number;
  windowWidthM: number;
  minimumDirectSolarReductionFactor: number;
  diffuseSolarShareUnaffected: number;
}

export interface WindowDto {
  id: number;
  roomId: number;
  areaM2: number;
  uValue: number;
  shgc: number;
  orientation: CardinalDirectionDto;
  shading: WindowShadingRequest;
}

export interface WindowApiResponse extends WindowDto {}

export interface UpsertWindowRequest {
  areaM2: number;
  uValue: number;
  shgc: number;
  orientation: CardinalDirectionDto;
  shading: WindowShadingRequest;
}

export interface ThermalZoneRoomDto {
  id: number;
  name: string;
  floorId: number;
}

export interface ThermalZoneDto {
  id: number;
  buildingId: number;
  name: string;
  rooms: ThermalZoneRoomDto[];
}

export interface ThermalZoneApiResponse extends ThermalZoneDto {}

export interface UpsertThermalZoneRequest {
  name: string;
  roomIds: number[];
}

export interface RoomVentilationParametersDto {
  airChangesPerHour: number;
  heatRecoveryEfficiency: number;
  infiltrationAirChangesPerHour: number;
  windExposureFactor: number;
  stackCoefficient: number;
  windCoefficient: number;
}

export type UpsertRoomVentilationParametersRequest = RoomVentilationParametersDto;

export interface RoomVentilationDefaultsDto extends RoomVentilationParametersDto {
  source?: string;
}

export interface NaturalVentilationPreviewRequest {
  indoorTemperatureC: number;
  outdoorTemperatureC: number;
  windSpeedMPerS: number;
  demandFactor: number;
  hourOfDay: number;
}

export interface NaturalVentilationPreviewDto {
  airflowM3PerHour?: number;
  airChangesPerHour?: number;
  stackFlowM3PerHour?: number;
  windFlowM3PerHour?: number;
  effectiveOpeningAreaM2?: number;
  isVentilationSufficient?: boolean;
}

export type GroundContactTypeDto =
  | "SlabOnGround"
  | "BasementConditioned"
  | "BasementUnconditioned"
  | "CrawlSpace"
  | "VentilatedCrawlSpace";

export interface RoomGroundContactDto {
  contactType: GroundContactTypeDto;
  exposedPerimeterM: number;
  burialDepthM: number;
  wallHeightBelowGradeM: number;
  horizontalInsulationWidthM: number;
  perimeterInsulationDepthM: number;
  underfloorVentilationAirChangesPerHour: number;
}

export type UpsertRoomGroundContactRequest = RoomGroundContactDto;
