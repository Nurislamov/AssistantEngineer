import { useQuery } from "@tanstack/react-query";
import { calculationsApi } from "@/entities/calculation/api/calculationsApi";
import { queryKeys } from "@/shared/api/queryKeys";

export function useEngineeringCoreDiagnosticsCatalog() {
  return useQuery({
    queryKey: queryKeys.calculations.engineeringCoreV1DiagnosticsCatalog,
    queryFn: () => calculationsApi.getEngineeringCoreV1DiagnosticsCatalog(),
    staleTime: 15 * 60 * 1000,
  });
}
