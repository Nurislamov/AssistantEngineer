import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Button } from "@mui/material";
import { Link as RouterLink, useParams } from "react-router-dom";
import { paths } from "@/app/router/paths";
import {
  useCachedBuildingCalculation,
  useCachedRoomCalculation,
} from "@/entities/calculation/model/useCachedCalculation";
import { CalculationResultView } from "@/features/calculations/calculation-result-view/ui/CalculationResultView";
import { parsePositiveIntParam } from "@/shared/lib/routeParams";
import { PageContainer } from "@/shared/ui/PageContainer";
import { PageHeader } from "@/shared/ui/PageHeader";

export function CalculationResultsPage(): JSX.Element {
  const params = useParams();
  const buildingId = parsePositiveIntParam(params.buildingId);
  const roomId = parsePositiveIntParam(params.roomId);
  const buildingResult = useCachedBuildingCalculation(buildingId);
  const roomResult = useCachedRoomCalculation(roomId);
  const result = buildingId ? buildingResult : roomResult;

  return (
    <PageContainer>
      <PageHeader
        title="Calculation Result"
        description="Cached result from the latest calculation run in this browser session."
        actions={
          <Button
            component={RouterLink}
            to={buildingId ? paths.buildingDetails(buildingId) : paths.buildings}
            startIcon={<ArrowBackIcon />}
          >
            Back
          </Button>
        }
      />
      <CalculationResultView result={result} />
    </PageContainer>
  );
}
