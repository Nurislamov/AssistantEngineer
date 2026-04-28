import { Alert } from "@mui/material";
import { backendEndpointAvailability } from "@/shared/api/apiRoutes";

export function EditBuildingForm(): JSX.Element {
  if (backendEndpointAvailability.buildings.update) {
    return <Alert severity="info">Форма редактирования здания ещё не подключена.</Alert>;
  }

  return (
    <Alert severity="info">
      Редактирование здания подключается после появления backend endpoint `PUT /api/v1/buildings/{'{buildingId}'}`.
    </Alert>
  );
}
