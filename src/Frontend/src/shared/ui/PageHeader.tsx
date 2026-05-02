import { Box, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";

interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: ReactNode;
}

export function PageHeader({ title, description, actions }: PageHeaderProps): JSX.Element {
  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={2}
      alignItems={{ xs: "stretch", sm: "flex-start" }}
      justifyContent="space-between"
      sx={{ mb: 3 }}
    >
      <Box>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 700, mb: 0.75 }}>
          {title}
        </Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}
      </Box>
      {actions ? <Box>{actions}</Box> : null}
    </Stack>
  );
}
