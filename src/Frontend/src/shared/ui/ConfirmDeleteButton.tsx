import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import { Button } from "@mui/material";
import { useState } from "react";

interface ConfirmDeleteButtonProps {
  disabled?: boolean;
  isDeleting?: boolean;
  onConfirm: () => void;
}

export function ConfirmDeleteButton({
  disabled = false,
  isDeleting = false,
  onConfirm,
}: ConfirmDeleteButtonProps): JSX.Element {
  const [needsConfirmation, setNeedsConfirmation] = useState(false);

  return (
    <Button
      color="error"
      variant={needsConfirmation ? "contained" : "outlined"}
      startIcon={<DeleteOutlineIcon />}
      disabled={disabled || isDeleting}
      onClick={() => {
        if (needsConfirmation) {
          onConfirm();
          setNeedsConfirmation(false);
          return;
        }

        setNeedsConfirmation(true);
      }}
    >
      {needsConfirmation ? "Подтвердить" : "Удалить"}
    </Button>
  );
}
