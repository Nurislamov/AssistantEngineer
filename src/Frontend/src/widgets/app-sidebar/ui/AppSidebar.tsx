import ApartmentIcon from "@mui/icons-material/Apartment";
import AssessmentIcon from "@mui/icons-material/Assessment";
import CalculateIcon from "@mui/icons-material/Calculate";
import DashboardIcon from "@mui/icons-material/Dashboard";
import PrecisionManufacturingIcon from "@mui/icons-material/PrecisionManufacturing";
import {
  Box,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
} from "@mui/material";
import { Link as RouterLink, useLocation } from "react-router-dom";
import { paths } from "@/app/router/paths";

export const appSidebarWidth = 260;

interface AppSidebarProps {
  mobileOpen: boolean;
  onClose: () => void;
}

const navItems = [
  { label: "Dashboard", to: paths.dashboard, icon: <DashboardIcon /> },
  { label: "Buildings", to: paths.buildings, icon: <ApartmentIcon /> },
  {
    label: "Calculations",
    to: paths.calculations,
    icon: <CalculateIcon />,
  },
  {
    label: "Reports",
    to: paths.reports,
    icon: <AssessmentIcon />,
  },
  {
    label: "Equipment",
    to: paths.equipmentSelection,
    icon: <PrecisionManufacturingIcon />,
  },
] as const;

function DrawerContent(): JSX.Element {
  const location = useLocation();

  return (
    <Box sx={{ height: "100%", borderRight: "1px solid", borderColor: "divider" }}>
      <Toolbar />
      <List sx={{ px: 1 }}>
        {navItems.map((item) => (
          <ListItemButton
            key={item.label}
            component={RouterLink}
            to={item.to}
            selected={
              item.to === paths.dashboard
                ? location.pathname === paths.dashboard
                : location.pathname.startsWith(item.to)
            }
            sx={{
              borderRadius: 1,
              mb: 0.5,
              "&.Mui-selected": {
                bgcolor: "primary.main",
                color: "primary.contrastText",
                "& .MuiListItemIcon-root": {
                  color: "inherit",
                },
                "&:hover": {
                  bgcolor: "primary.dark",
                },
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </Box>
  );
}

export function AppSidebar({ mobileOpen, onClose }: AppSidebarProps): JSX.Element {
  return (
    <>
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onClose}
        ModalProps={{ keepMounted: true }}
        sx={{
          display: { xs: "block", md: "none" },
          "& .MuiDrawer-paper": { width: appSidebarWidth },
        }}
      >
        <DrawerContent />
      </Drawer>
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: "none", md: "block" },
          width: appSidebarWidth,
          flexShrink: 0,
          "& .MuiDrawer-paper": {
            width: appSidebarWidth,
            boxSizing: "border-box",
            borderRight: 0,
          },
        }}
        open
      >
        <DrawerContent />
      </Drawer>
    </>
  );
}
