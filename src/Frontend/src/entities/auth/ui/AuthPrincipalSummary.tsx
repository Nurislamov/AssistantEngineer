import { Chip, Stack, Typography } from "@mui/material";
import type { AuthStateViewModel } from "../model/authTypes";

interface AuthPrincipalSummaryProps {
  authState: AuthStateViewModel;
}

export function AuthPrincipalSummary({
  authState,
}: AuthPrincipalSummaryProps): JSX.Element {
  const { principal, organization } = authState;

  if (!principal.isAuthenticated) {
    return (
      <Typography variant="caption" color="text.secondary">
        Principal is anonymous in current session.
      </Typography>
    );
  }

  return (
    <Stack spacing={0.5}>
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        Authenticated principal
      </Typography>
      <Typography variant="caption" color="text.secondary">
        {principal.displayName ?? principal.email ?? principal.externalSubjectId ?? "Authenticated user"}
      </Typography>
      {organization?.organizationName || organization?.organizationSlug ? (
        <Typography variant="caption" color="text.secondary">
          Organization: {organization.organizationName ?? organization.organizationSlug}
        </Typography>
      ) : null}
      <Stack direction="row" spacing={0.5} flexWrap="wrap">
        <Chip size="small" variant="outlined" label={`Roles: ${principal.roles.length}`} />
        <Chip size="small" variant="outlined" label={`Permissions: ${principal.permissions.length}`} />
      </Stack>
    </Stack>
  );
}
