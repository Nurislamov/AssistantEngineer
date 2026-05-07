import { Box, Typography } from "@mui/material";
import { useMemo } from "react";

export function JsonBlock({ title, value }: { title: string; value: unknown }): JSX.Element {
  const text = useMemo(() => JSON.stringify(value, null, 2), [value]);

  return (
    <Box>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>{title}</Typography>
      <Box component="pre" sx={{ m: 0, p: 2, bgcolor: "grey.100", borderRadius: 1, overflow: "auto", fontSize: 12 }}>
        {text}
      </Box>
    </Box>
  );
}
