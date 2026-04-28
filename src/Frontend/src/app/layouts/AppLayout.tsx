import { Box, Toolbar } from "@mui/material";
import { useState } from "react";
import { Outlet } from "react-router-dom";
import { AppHeader } from "@/widgets/app-header/ui/AppHeader";
import { AppSidebar, appSidebarWidth } from "@/widgets/app-sidebar/ui/AppSidebar";

export function AppLayout(): JSX.Element {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      <AppHeader onMenuClick={() => setMobileOpen(true)} />
      <AppSidebar mobileOpen={mobileOpen} onClose={() => setMobileOpen(false)} />
      <Box
        sx={{
          flexGrow: 1,
          minWidth: 0,
          width: { md: `calc(100% - ${appSidebarWidth}px)` },
        }}
      >
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}
