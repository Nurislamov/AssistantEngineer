import { Alert } from "@mui/material";
import { backendEndpointAvailability } from "@/shared/api/apiRoutes";

export function EditRoomForm(): JSX.Element {
  if (backendEndpointAvailability.rooms.update) {
    return <Alert severity="info">Форма редактирования помещения ещё не подключена.</Alert>;
  }

  return (
    <Alert severity="info">
      Редактирование помещения подключается после появления backend endpoint `PUT /api/v1/rooms/{'{roomId}'}`.
    </Alert>
  );
}
