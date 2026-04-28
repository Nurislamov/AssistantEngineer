import { Box, CircularProgress, Stack, Typography } from "@mui/material";

interface LoadingStateProps {
  label?: string;
}

export function LoadingState({ label = "Загрузка данных" }: LoadingStateProps): JSX.Element {
  return (
    <Box sx={{ py: 5 }}>
      <Stack spacing={2} alignItems="center">
        <CircularProgress size={28} />
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
      </Stack>
    </Box>
  );
}
