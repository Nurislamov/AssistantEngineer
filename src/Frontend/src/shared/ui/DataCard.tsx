import { Paper } from "@mui/material";
import type { PropsWithChildren } from "react";

interface DataCardProps extends PropsWithChildren {
  compact?: boolean;
}

export function DataCard({ children, compact = false }: DataCardProps): JSX.Element {
  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 1,
        p: compact ? 2 : 3,
        bgcolor: "background.paper",
      }}
    >
      {children}
    </Paper>
  );
}
