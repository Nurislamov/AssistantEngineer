import { useQueryClient } from "@tanstack/react-query";
import type { CalculationResultDto } from "@/entities/calculation/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useCachedBuildingCalculation(
  buildingId: number | null,
): CalculationResultDto | undefined {
  const queryClient = useQueryClient();
  if (!buildingId) {
    return undefined;
  }

  return queryClient.getQueryData<CalculationResultDto>(
    queryKeys.calculations.buildingLatest(buildingId),
  );
}

export function useCachedRoomCalculation(roomId: number | null): CalculationResultDto | undefined {
  const queryClient = useQueryClient();
  if (!roomId) {
    return undefined;
  }

  return queryClient.getQueryData<CalculationResultDto>(queryKeys.calculations.roomLatest(roomId));
}
