export interface EquipmentCatalogItemDto {
  id: number;
  manufacturer: string;
  systemType: string;
  unitType: string;
  modelName: string;
  nominalCoolingCapacityKw: number;
  isActive: boolean;
}

export type EquipmentCatalogItemApiResponse = EquipmentCatalogItemDto;

export interface UpsertEquipmentCatalogItemRequest {
  manufacturer: string;
  systemType: string;
  unitType: string;
  modelName: string;
  nominalCoolingCapacityKw: number;
  isActive: boolean;
}

export interface EquipmentSelectionRequest {
  systemType: string;
  unitType: string;
}

export interface EquipmentSelectionResultDto {
  roomId: number;
  totalHeatLoadKw: number;
  designCapacityKw: number;
  requestedSystemType: string;
  requestedUnitType: string;
  selectedCatalogItemId: number;
  selectedManufacturer: string;
  selectedModelName: string;
  selectedNominalCoolingCapacityKw: number;
  capacityReserveKw: number;
}
