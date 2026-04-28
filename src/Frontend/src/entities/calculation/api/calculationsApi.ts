import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import { calculationMethods } from "@/shared/constants/calculationMethods";
import { mapBuildingCalculationResult, mapRoomCalculationResult } from "../lib/calculationAdapters";
import type {
  BuildingCoolingLoadApiResponse,
  BuildingEnergyBalanceApiResponse,
  BuildingHeatingLoadApiResponse,
  CalculationResultDto,
  RoomCoolingLoadApiResponse,
  RoomHeatingLoadApiResponse,
} from "../types";

export const calculationsApi = {
  async runBuildingHeatLoadCalculation(buildingId: number): Promise<CalculationResultDto> {
    const [cooling, heating] = await Promise.all([
      apiRequest<BuildingCoolingLoadApiResponse>(
        apiRoutes.calculations.buildingCoolingLoad(buildingId),
        {
          query: { method: calculationMethods.cooling },
        },
      ),
      apiRequest<BuildingHeatingLoadApiResponse>(
        apiRoutes.calculations.buildingHeatingLoad(buildingId),
        {
          query: { method: calculationMethods.heating },
        },
      ),
    ]);

    return mapBuildingCalculationResult(cooling, heating);
  },

  async runRoomHeatLoadCalculation(roomId: number): Promise<CalculationResultDto> {
    const [cooling, heating] = await Promise.all([
      apiRequest<RoomCoolingLoadApiResponse>(apiRoutes.calculations.roomCoolingLoad(roomId), {
        query: { method: calculationMethods.cooling },
      }),
      apiRequest<RoomHeatingLoadApiResponse>(apiRoutes.calculations.roomHeatingLoad(roomId), {
        query: { method: calculationMethods.heating },
      }),
    ]);

    return mapRoomCalculationResult(cooling, heating);
  },

  async runBuildingEnergyBalance(buildingId: number): Promise<BuildingEnergyBalanceApiResponse> {
    return apiRequest<BuildingEnergyBalanceApiResponse>(
      apiRoutes.calculations.buildingEnergyBalance(buildingId),
      {
        query: {
          coolingMethod: calculationMethods.cooling,
          heatingMethod: calculationMethods.heating,
        },
      },
    );
  },
};
