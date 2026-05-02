import CalculateIcon from "@mui/icons-material/Calculate";
import { Button } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { paths } from "@/app/router/paths";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { useRunBuildingCalculation } from "../model/useRunBuildingCalculation";

interface RunBuildingCalculationButtonProps {
  buildingId: number;
  onError?: (message: string) => void;
}

export function RunBuildingCalculationButton({
  buildingId,
  onError,
}: RunBuildingCalculationButtonProps): JSX.Element {
  const navigate = useNavigate();
  const runCalculation = useRunBuildingCalculation(buildingId);

  return (
    <Button
      variant="contained"
      startIcon={<CalculateIcon />}
      disabled={runCalculation.isPending}
      onClick={() =>
        runCalculation.mutate(undefined, {
          onSuccess: () => navigate(paths.buildingCalculationResult(buildingId)),
          onError: (error) => onError?.(getErrorMessage(error)),
        })
      }
    >
      {runCalculation.isPending ? "Running..." : "Run calculation"}
    </Button>
  );
}
