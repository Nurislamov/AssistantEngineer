import MenuIcon from "@mui/icons-material/Menu";
import { AppBar, Box, IconButton, Toolbar, Typography } from "@mui/material";
import { ProjectSelector } from "@/features/projects/project-selection/ui/ProjectSelector";

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
      <Toolbar sx={{ gap: 2 }}>
        <IconButton
          edge="start"
          color="inherit"
          aria-label="Open menu"
          onClick={onMenuClick}
          sx={{ display: { md: "none" }, mr: 1 }}
        >
          <MenuIcon />
        </IconButton>
        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <Typography variant="h6" component="div" sx={{ fontWeight: 700, lineHeight: 1.2 }}>
            AssistantEngineer
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Building engineering calculations
          </Typography>
        </Box>
        <ProjectSelector />
      </Toolbar>
    </AppBar>
  );
}
