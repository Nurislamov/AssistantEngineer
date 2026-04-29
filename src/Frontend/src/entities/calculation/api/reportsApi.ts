import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiBlob, apiRequest } from "@/shared/api/httpClient";
import { calculationMethods } from "@/shared/constants/calculationMethods";

export const reportsApi = {
  async downloadBuildingEnergyBalanceExcel(buildingId: number): Promise<Blob> {
    return apiBlob(apiRoutes.reports.buildingEnergyBalanceExcel(buildingId), {
      query: {
        coolingMethod: calculationMethods.cooling,
        heatingMethod: calculationMethods.heating,
      },
    });
  },

  async downloadBuildingCoolingExcel(buildingId: number): Promise<Blob> {
    return apiBlob(apiRoutes.reports.buildingCoolingExcel(buildingId), {
      query: {
        method: calculationMethods.cooling,
      },
    });
  },

  async getBuildingCoolingReport(buildingId: number): Promise<unknown> {
    return apiRequest<unknown>(apiRoutes.reports.buildingCooling(buildingId), {
      query: {
        method: calculationMethods.cooling,
      },
    });
  },

  async getBuildingHeatingReport(buildingId: number): Promise<unknown> {
    return apiRequest<unknown>(apiRoutes.reports.buildingHeating(buildingId), {
      query: {
        method: calculationMethods.heating,
      },
    });
  },
};
