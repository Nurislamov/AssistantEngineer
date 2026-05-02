import { useQuery } from "@tanstack/react-query";
import { calculationsApi } from "@/entities/calculation/api/calculationsApi";
import { queryKeys } from "@/shared/api/queryKeys";

export function useEngineeringCoreStatus() {
  return useQuery({
    queryKey: queryKeys.calculations.engineeringCoreV1Status,
    queryFn: () => calculationsApi.getEngineeringCoreV1Status(),
    staleTime: 15 * 60 * 1000,
  });
}
