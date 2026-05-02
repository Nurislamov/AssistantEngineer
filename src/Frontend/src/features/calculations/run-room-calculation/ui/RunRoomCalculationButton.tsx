import CalculateIcon from "@mui/icons-material/Calculate";
import { IconButton, Tooltip } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { paths } from "@/app/router/paths";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { useRunRoomCalculation } from "../model/useRunRoomCalculation";

interface RunRoomCalculationButtonProps {
  roomId: number;
  onError?: (message: string) => void;
}

export function RunRoomCalculationButton({
  roomId,
  onError,
}: RunRoomCalculationButtonProps): JSX.Element {
  const navigate = useNavigate();
  const runCalculation = useRunRoomCalculation(roomId);

  return (
    <Tooltip title="Запустить расчёт помещения">
      <span>
        <IconButton
          color="primary"
          disabled={runCalculation.isPending}
          onClick={() =>
            runCalculation.mutate(undefined, {
              onSuccess: () => navigate(paths.roomCalculationResult(roomId)),
              onError: (error) => onError?.(getErrorMessage(error)),
            })
          }
        >
          <CalculateIcon />
        </IconButton>
      </span>
    </Tooltip>
  );
}
