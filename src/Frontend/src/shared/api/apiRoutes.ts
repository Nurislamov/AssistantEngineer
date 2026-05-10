import { appConfig } from "@/shared/config/env";

const apiPrefix = `/api/v${appConfig.apiVersion}`;

export const apiRoutes = {
  projects: {
    list: () => `${apiPrefix}/projects`,
    byId: (projectId: number) => `${apiPrefix}/projects/${projectId}`,
    create: () => `${apiPrefix}/projects`,
    update: (projectId: number) => `${apiPrefix}/projects/${projectId}`,
    delete: (projectId: number) => `${apiPrefix}/projects/${projectId}`,
  },
  buildings: {
    listByProject: (projectId: number) => `${apiPrefix}/projects/${projectId}/buildings`,
    byId: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}`,
    create: (projectId: number) => `${apiPrefix}/projects/${projectId}/buildings`,
    update: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}`,
    delete: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}`,
    readiness: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/readiness`,
    validation: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/validation`,
  },
  floors: {
    listByBuilding: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/floors`,
    byId: (floorId: number) => `${apiPrefix}/floors/${floorId}`,
    create: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/floors`,
    update: (floorId: number) => `${apiPrefix}/floors/${floorId}`,
    delete: (floorId: number) => `${apiPrefix}/floors/${floorId}`,
  },
  rooms: {
    list: () => `${apiPrefix}/rooms`,
    listByBuilding: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/rooms`,
    byId: (roomId: number) => `${apiPrefix}/rooms/${roomId}`,
    create: () => `${apiPrefix}/rooms`,
    update: (roomId: number) => `${apiPrefix}/rooms/${roomId}`,
    delete: (roomId: number) => `${apiPrefix}/rooms/${roomId}`,
    walls: (roomId: number) => `${apiPrefix}/rooms/${roomId}/walls`,
    wall: (roomId: number, wallId: number) => `${apiPrefix}/rooms/${roomId}/walls/${wallId}`,
    windows: (roomId: number) => `${apiPrefix}/rooms/${roomId}/windows`,
    window: (roomId: number, windowId: number) =>
      `${apiPrefix}/rooms/${roomId}/windows/${windowId}`,
    ventilation: (roomId: number) => `${apiPrefix}/rooms/${roomId}/ventilation-parameters`,
    ventilationDefaults: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/ventilation-parameters/defaults`,
    applyVentilationDefaults: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/ventilation-parameters/apply-defaults`,
    naturalVentilationPreview: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/natural-ventilation/preview`,
    groundContact: (roomId: number) => `${apiPrefix}/rooms/${roomId}/ground-contact`,
    equipmentSelection: (roomId: number) => `${apiPrefix}/rooms/${roomId}/equipment-selection`,
  },
  thermalZones: {
    listByBuilding: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/thermal-zones`,
    byId: (zoneId: number) => `${apiPrefix}/thermal-zones/${zoneId}`,
    create: (buildingId: number) => `${apiPrefix}/buildings/${buildingId}/thermal-zones`,
    update: (zoneId: number) => `${apiPrefix}/thermal-zones/${zoneId}`,
    delete: (zoneId: number) => `${apiPrefix}/thermal-zones/${zoneId}`,
  },
  equipmentCatalog: {
    list: () => `${apiPrefix}/equipment-catalog`,
    byId: (id: number) => `${apiPrefix}/equipment-catalog/${id}`,
    create: () => `${apiPrefix}/equipment-catalog`,
    update: (id: number) => `${apiPrefix}/equipment-catalog/${id}`,
    delete: (id: number) => `${apiPrefix}/equipment-catalog/${id}`,
  },
  calculations: {
    buildingCoolingLoad: (buildingId: number) =>
      `${apiPrefix}/buildings/${buildingId}/load-calculations/cooling-load`,
    buildingHeatingLoad: (buildingId: number) =>
      `${apiPrefix}/buildings/${buildingId}/load-calculations/heating-load`,
    buildingEnergyBalance: (buildingId: number) =>
      `${apiPrefix}/buildings/${buildingId}/load-calculations/energy-balance`,
    engineeringCoreV1Status: () => `${apiPrefix}/calculations/engineering-core/v1/status`,
    engineeringCoreV1DiagnosticsCatalog: () => `${apiPrefix}/calculations/engineering-core/v1/diagnostics-catalog`,
    roomCoolingLoad: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/load-calculations/cooling-load`,
    roomHeatingLoad: (roomId: number) =>
      `${apiPrefix}/rooms/${roomId}/load-calculations/heating-load`,
  },
  reports: {
    buildingEnergyBalanceExcel: (buildingId: number) =>
      `${apiPrefix}/reports/buildings/${buildingId}/energy-balance/excel`,
    buildingCooling: (buildingId: number) => `${apiPrefix}/reports/buildings/${buildingId}/cooling`,
    buildingCoolingExcel: (buildingId: number) =>
      `${apiPrefix}/reports/buildings/${buildingId}/cooling/excel`,
    buildingHeating: (buildingId: number) => `${apiPrefix}/reports/buildings/${buildingId}/heating`,
  },
  engineeringWorkflow: {
    state: (projectId: number) =>
      `${apiPrefix}/engineering-workflow/${projectId}/state`,
    validate: () => `${apiPrefix}/engineering-workflow/validate`,
    prepareCalculation: () => `${apiPrefix}/engineering-workflow/prepare-calculation`,
    runCalculation: () => `${apiPrefix}/engineering-workflow/run-calculation`,
    tracePreview: () => `${apiPrefix}/engineering-workflow/trace-preview`,
    report: () => `${apiPrefix}/engineering-workflow/report`,
    reportExportJson: () => `${apiPrefix}/engineering-workflow/report/export/json`,
    reportExportMarkdown: () => `${apiPrefix}/engineering-workflow/report/export/markdown`,
  },
} as const;

export const backendEndpointAvailability = {
  buildings: {
    update: true,
    delete: true,
  },
  rooms: {
    update: true,
    delete: true,
    listByBuilding: true,
  },
  calculations: {
    getById: false,
    latestByBuilding: false,
  },
} as const;





