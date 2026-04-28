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
  },
  calculations: {
    buildingLatest: (buildingId: number) => ["calculations", "building", buildingId, "latest"] as const,
    roomLatest: (roomId: number) => ["calculations", "room", roomId, "latest"] as const,
  },
} as const;
