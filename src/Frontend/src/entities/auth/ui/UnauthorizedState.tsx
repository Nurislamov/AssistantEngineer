import LoginIcon from "@mui/icons-material/Login";
import { Alert, Button, Stack, Typography } from "@mui/material";
import { buildUnauthorizedMessage } from "../model/authViewModel";

interface UnauthorizedStateProps {
  message?: string;
  showLoginPlaceholder?: boolean;
}

export function UnauthorizedState({
  message,
  showLoginPlaceholder = true,
}: UnauthorizedStateProps): JSX.Element {
  return (
    <Stack spacing={1}>
      <Alert severity="warning">{message ?? buildUnauthorizedMessage()}</Alert>
      {showLoginPlaceholder ? (
        <Button
          variant="outlined"
          startIcon={<LoginIcon />}
          disabled
          aria-label="Login placeholder is not connected"
        >
          Login integration pending
        </Button>
      ) : null}
      <Typography variant="caption" color="text.secondary">
        Auth shell foundation is active. External provider integration is not connected in this stage.
      </Typography>
    </Stack>
  );
}
