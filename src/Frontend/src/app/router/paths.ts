export const paths = {
  dashboard: "/",
  buildings: "/buildings",
  calculations: "/calculations",
  buildingDetails: (buildingId: number | string) => `/buildings/${buildingId}`,
  buildingCalculationResult: (buildingId: number | string) =>
    `/calculations/buildings/${buildingId}/latest`,
  roomCalculationResult: (roomId: number | string) => `/calculations/rooms/${roomId}/latest`,
  equipmentSelection: "/equipment-selection",
} as const;
