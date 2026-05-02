import { useMutation, useQueryClient } from "@tanstack/react-query";
import { calculationsApi } from "@/entities/calculation/api/calculationsApi";
import type { CalculationResultDto } from "@/entities/calculation/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useRunRoomCalculation(roomId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => calculationsApi.runRoomHeatLoadCalculation(roomId),
    onSuccess: (result: CalculationResultDto) => {
      queryClient.setQueryData(queryKeys.calculations.roomLatest(roomId), result);
    },
  });
}
