import { useMutation } from "@tanstack/react-query";
import { calculationsApi } from "@/entities/calculation/api/calculationsApi";

export type CalculationTarget = "building-loads" | "building-balance" | "room-loads";

export function useBuildingCalculationExecution(buildingId: number, selectedRoomId: number) {
  return useMutation({
    mutationFn: async (target: CalculationTarget) => {
      if (target === "building-loads") return calculationsApi.runBuildingHeatLoadCalculation(buildingId);
      if (target === "building-balance") return calculationsApi.runBuildingEnergyBalance(buildingId);
      return calculationsApi.runRoomHeatLoadCalculation(selectedRoomId);
    },
  });
}
