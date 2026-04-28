import { Box } from "@mui/material";
import type { PropsWithChildren } from "react";

export function PageContainer({ children }: PropsWithChildren): JSX.Element {
  return (
    <Box
      component="main"
      sx={{
        width: "100%",
        maxWidth: 1320,
        mx: "auto",
        px: { xs: 2, md: 3 },
        py: { xs: 2, md: 3 },
      }}
    >
      {children}
    </Box>
  );
}
