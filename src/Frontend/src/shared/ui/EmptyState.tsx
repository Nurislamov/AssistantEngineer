import InboxOutlinedIcon from "@mui/icons-material/InboxOutlined";
import { Box, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";

interface EmptyStateProps {
  title: string;
  description?: string;
  actions?: ReactNode;
}

export function EmptyState({ title, description, actions }: EmptyStateProps): JSX.Element {
  return (
    <Box
      sx={{
        border: "1px dashed",
        borderColor: "divider",
        borderRadius: 1,
        p: 4,
        bgcolor: "background.paper",
      }}
    >
      <Stack spacing={1.5} alignItems="center" textAlign="center">
        <InboxOutlinedIcon color="disabled" />
        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
          {title}
        </Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}
        {actions}
      </Stack>
    </Box>
  );
}
