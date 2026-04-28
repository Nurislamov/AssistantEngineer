import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiBlob } from "@/shared/api/httpClient";
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
};
