import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "@/app/layouts/AppLayout";
import { paths } from "@/app/router/paths";
import { BuildingDetailsPage } from "@/pages/building-details/ui/BuildingDetailsPage";
import { BuildingsPage } from "@/pages/buildings/ui/BuildingsPage";
import { CalculationResultsPage } from "@/pages/calculation-results/ui/CalculationResultsPage";
import { DashboardPage } from "@/pages/dashboard/ui/DashboardPage";
import { EquipmentSelectionPage } from "@/pages/equipment-selection/ui/EquipmentSelectionPage";
import { ReportsPage } from "@/pages/reports/ui/ReportsPage";

export function AppRouter(): JSX.Element {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path={paths.dashboard} element={<DashboardPage />} />
        <Route path={paths.buildings} element={<BuildingsPage />} />
        <Route path={paths.calculations} element={<CalculationResultsPage />} />
        <Route path={paths.reports} element={<ReportsPage />} />
        <Route path="/buildings/:buildingId" element={<BuildingDetailsPage />} />
        <Route
          path="/calculations/buildings/:buildingId/latest"
          element={<CalculationResultsPage />}
        />
        <Route path="/calculations/rooms/:roomId/latest" element={<CalculationResultsPage />} />
        <Route path={paths.equipmentSelection} element={<EquipmentSelectionPage />} />
        <Route path="*" element={<Navigate to={paths.dashboard} replace />} />
      </Route>
    </Routes>
  );
}
