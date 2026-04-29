export const queryKeys = {
  projects: {
    all: ["projects"] as const,
  },
  buildings: {
    all: ["buildings"] as const,
    byProject: (projectId: number) => ["buildings", "project", projectId] as const,
    detail: (buildingId: number) => ["buildings", "detail", buildingId] as const,
  },
  floors: {
    byBuilding: (buildingId: number) => ["floors", "building", buildingId] as const,
  },
  rooms: {
    byBuilding: (buildingId: number) => ["rooms", "building", buildingId] as const,
    detail: (roomId: number) => ["rooms", "detail", roomId] as const,
    walls: (roomId: number) => ["rooms", roomId, "walls"] as const,
    windows: (roomId: number) => ["rooms", roomId, "windows"] as const,
    ventilation: (roomId: number) => ["rooms", roomId, "ventilation"] as const,
    groundContact: (roomId: number) => ["rooms", roomId, "ground-contact"] as const,
  },
  thermalZones: {
    byBuilding: (buildingId: number) => ["thermal-zones", "building", buildingId] as const,
  },
  equipmentCatalog: {
    all: ["equipment-catalog"] as const,
  },
  calculations: {
    buildingLatest: (buildingId: number) => ["calculations", "building", buildingId, "latest"] as const,
    roomLatest: (roomId: number) => ["calculations", "room", roomId, "latest"] as const,
  },
} as const;
