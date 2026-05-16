import { Stack, Typography } from "@mui/material";
import { getSecurityNonClaims } from "../model/authViewModel";

export function SecurityNonClaimsPanel(): JSX.Element {
  const nonClaims = getSecurityNonClaims();

  return (
    <Stack spacing={0.5}>
      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
        Security non-claims
      </Typography>
      {nonClaims.map((item) => (
        <Typography key={item} variant="caption" color="text.secondary">
          - {item}
        </Typography>
      ))}
    </Stack>
  );
}
