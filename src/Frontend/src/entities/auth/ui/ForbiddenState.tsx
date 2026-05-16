import { Alert, Stack, Typography } from "@mui/material";
import { buildForbiddenMessage } from "../model/authViewModel";

interface ForbiddenStateProps {
  requiredPermission?: string;
}

export function ForbiddenState({
  requiredPermission,
}: ForbiddenStateProps): JSX.Element {
  return (
    <Stack spacing={1}>
      <Alert severity="error">{buildForbiddenMessage(requiredPermission)}</Alert>
      <Typography variant="caption" color="text.secondary">
        Principal context is authenticated but does not satisfy required permission for this resource.
      </Typography>
    </Stack>
  );
}
