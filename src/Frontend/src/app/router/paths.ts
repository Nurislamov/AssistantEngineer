export const paths = {
  dashboard: "/",
  buildings: "/buildings",
  engineeringWorkflow: "/engineering-workflow",
  reports: "/reports",
  calculations: "/calculations",
  buildingDetails: (buildingId: number | string) => `/buildings/${buildingId}`,
  buildingCalculationResult: (buildingId: number | string) =>
    `/calculations/buildings/${buildingId}/latest`,
  roomCalculationResult: (roomId: number | string) => `/calculations/rooms/${roomId}/latest`,
  equipmentSelection: "/equipment-selection",
  equipmentDiagnostics: "/equipment-diagnostics",
} as const;
