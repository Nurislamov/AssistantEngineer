import { Alert, Stack, Typography } from "@mui/material";
import { useAuth } from "../model/useAuth";
import { getAuthStatusDescription, getAuthStatusLabel } from "../model/authViewModel";
import { AuthPrincipalSummary } from "./AuthPrincipalSummary";

export function AuthStatusBanner(): JSX.Element {
  const { authState } = useAuth();
  const severity =
    authState.status === "forbidden"
      ? "error"
      : authState.status === "unauthorized"
        ? "warning"
        : authState.status === "authenticated"
          ? "success"
          : "info";

  return (
    <Alert severity={severity}>
      <Stack spacing={0.75}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {getAuthStatusLabel(authState.status)}
        </Typography>
        <Typography variant="body2">{getAuthStatusDescription(authState)}</Typography>
        {authState.developmentMode && !authState.authRequired ? (
          <Typography variant="caption">
            Development anonymous mode is enabled. Auth foundation only; external provider is not connected.
          </Typography>
        ) : null}
        <AuthPrincipalSummary authState={authState} />
      </Stack>
    </Alert>
  );
}
