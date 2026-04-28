import { appConfig } from "@/shared/config/env";

const apiPrefix = `/api/v${appConfig.apiVersion}`;

export const apiRoutes = {
  projects: {
    list: () => `${apiPrefix}/projects`,
    byId: (projectId: number) => `${apiPrefix}/projects/${projectId}`,
    create: () => `${apiPrefix}/projects`,
  },
  buildings: {
    listByProject: (projectId: number) => `${apiPrefix}/projects/${projectId}/buildings`,
    byId: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}`,
    create: (projectId: number) => `${apiPrefix}/projects/${projectId}/buildings`,
  },
  floors: {
    listByBuilding: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/floors`,
    byId: (floorId: number) => `${apiPrefix}/floors/${floorId}`,
    create: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/floors`,
  },
  rooms: {
    list: () => `${apiPrefix}/rooms`,
    byId: (roomId: number) => `${apiPrefix}/rooms/${roomId}`,
    create: () => `${apiPrefix}/rooms`,
  },
  calculations: {
    buildingCoolingLoad: (buildingId: number) =>
      `${apiPrefix}/buildings/${buildingId}/load-calculations/cooling-load`,
    buildingHeatingLoad: (buildingId: number) =>
      `${apiPrefix}/buildings/${buildingId}/load-calculations/heating-load`,
    buildingEnergyBalance: (buildingId: number) =>
      `${apiPrefix}/buildings/${buildingId}/load-calculations/energy-balance`,
    roomCoolingLoad: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/load-calculations/cooling-load`,
    roomHeatingLoad: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/load-calculations/heating-load`,
  },
  reports: {
    buildingEnergyBalanceExcel: (buildingId: number) =>
      `${apiPrefix}/reports/buildings/${buildingId}/energy-balance/excel`,
  },
} as const;

export const backendEndpointAvailability = {
  buildings: {
    update: false,
    delete: false,
  },
  rooms: {
    update: false,
    delete: false,
    listByBuilding: false,
  },
  calculations: {
    getById: false,
    latestByBuilding: false,
  },
} as const;
