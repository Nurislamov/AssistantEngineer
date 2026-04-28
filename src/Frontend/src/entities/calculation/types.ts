export interface RoomCalculationResultDto {
  roomId: number;
  roomName: string;
  heatLoss?: number;
  heatGain?: number;
  coolingLoad?: number;
  heatingLoad?: number;
}

export interface CalculationResultDto {
  id: number;
  buildingId?: number;
  buildingName?: string;
  roomId?: number;
  roomName?: string;
  totalHeatLoss?: number;
  totalHeatGain?: number;
  coolingLoad?: number;
  heatingLoad?: number;
  calculatedAt?: string;
  rooms?: RoomCalculationResultDto[];
}

export interface ThermalZoneCalculationApiResponse {
  thermalZoneId?: number | null;
  thermalZoneName: string;
  isUnassignedRoomsZone: boolean;
  roomsCount: number;
  peakHour?: number | null;
  totalHeatLoadW: number;
  totalHeatLoadKw: number;
  roomIds: number[];
  hourlyHeatLoadW: number[];
}

export interface BuildingCoolingLoadApiResponse {
  buildingId: number;
  buildingName: string;
  calculationMethod: string;
  peakHour?: number | null;
  floorsCount: number;
  roomsCount: number;
  totalHeatLoadW: number;
  totalHeatLoadKw: number;
  designReserveFactor: number;
  designCapacityW: number;
  designCapacityKw: number;
  hourlyHeatLoadW: number[];
  thermalZones: ThermalZoneCalculationApiResponse[];
}

export interface RoomCoolingLoadApiResponse {
  roomId: number;
  roomName: string;
  calculationMethod: string;
  peakHour?: number | null;
  areaM2: number;
  heightM: number;
  volumeM3: number;
  indoorTemperatureC: number;
  outdoorTemperatureC: number;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  totalHeatLoadW: number;
  totalHeatLoadKw: number;
  designCapacityW: number;
  designCapacityKw: number;
  hourlyHeatLoadW: number[];
}

export interface RoomHeatingLoadApiResponse {
  roomId: number;
  roomName: string;
  calculationMethod: string;
  indoorDesignTemperatureC: number;
  outdoorDesignTemperatureC: number;
  deltaTemperatureC: number;
  volumeM3: number;
  airChangesPerHour: number;
  transmissionHeatLossW: number;
  ventilationHeatLossW: number;
  totalDesignHeatingLoadW: number;
  totalDesignHeatingLoadKw: number;
}

export interface BuildingHeatingLoadApiResponse {
  buildingId: number;
  projectName: string;
  buildingName: string;
  calculationMethod: string;
  roomsCount: number;
  transmissionHeatLossW: number;
  ventilationHeatLossW: number;
  totalDesignHeatingLoadW: number;
  totalDesignHeatingLoadKw: number;
  rooms: RoomHeatingLoadApiResponse[];
}

export interface MonthlyEnergyBalanceApiResponse {
  month: number;
  coolingDemandKWh: number;
  heatingDemandKWh: number;
}

export interface BuildingEnergyBalanceApiResponse {
  buildingId: number;
  buildingName: string;
  coolingCalculationMethod: string;
  heatingCalculationMethod: string;
  annualCoolingDemandKWh: number;
  annualHeatingDemandKWh: number;
  annualTotalDemandKWh: number;
  monthlyBalances: MonthlyEnergyBalanceApiResponse[];
}
