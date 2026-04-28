import { useMutation, useQueryClient } from "@tanstack/react-query";
import { calculationsApi } from "@/entities/calculation/api/calculationsApi";
import type { CalculationResultDto } from "@/entities/calculation/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useRunBuildingCalculation(buildingId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => calculationsApi.runBuildingHeatLoadCalculation(buildingId),
    onSuccess: (result: CalculationResultDto) => {
      queryClient.setQueryData(queryKeys.calculations.buildingLatest(buildingId), result);
    },
  });
}
