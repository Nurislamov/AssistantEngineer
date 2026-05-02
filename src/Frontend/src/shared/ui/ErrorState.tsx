import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import { Alert, Button, Stack } from "@mui/material";

interface ErrorStateProps {
  title?: string;
  message: string;
  onRetry?: () => void;
}

export function ErrorState({ title = "Ошибка", message, onRetry }: ErrorStateProps): JSX.Element {
  return (
    <Alert
      severity="error"
      icon={<ErrorOutlineIcon />}
      action={
        onRetry ? (
          <Button color="inherit" size="small" onClick={onRetry}>
            Повторить
          </Button>
        ) : undefined
      }
    >
      <Stack spacing={0.5}>
        <strong>{title}</strong>
        <span>{message}</span>
      </Stack>
    </Alert>
  );
}
