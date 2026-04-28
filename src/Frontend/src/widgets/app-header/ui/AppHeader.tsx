import MenuIcon from "@mui/icons-material/Menu";
import { AppBar, Box, IconButton, Toolbar, Typography } from "@mui/material";

interface AppHeaderProps {
  onMenuClick: () => void;
}

export function AppHeader({ onMenuClick }: AppHeaderProps): JSX.Element {
  return (
    <AppBar
      position="fixed"
      color="inherit"
      elevation={0}
      sx={{
        borderBottom: "1px solid",
        borderColor: "divider",
        zIndex: (theme) => theme.zIndex.drawer + 1,
      }}
    >
      <Toolbar>
        <IconButton
          edge="start"
          color="inherit"
          aria-label="Открыть меню"
          onClick={onMenuClick}
          sx={{ display: { md: "none" }, mr: 1 }}
        >
          <MenuIcon />
        </IconButton>
        <Box>
          <Typography variant="h6" component="div" sx={{ fontWeight: 700, lineHeight: 1.2 }}>
            AssistantEngineer
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Инженерные расчёты зданий
          </Typography>
        </Box>
      </Toolbar>
    </AppBar>
  );
}
