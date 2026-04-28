import {
  Alert,
  Button,
  MenuItem,
  Stack,
  TextField,
} from "@mui/material";
import { FormEvent, useEffect, useState } from "react";
import type { FloorDto } from "@/entities/floor/types";
import type { CreateRoomRequest } from "@/entities/room/types";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import {
  createDefaultRoomForm,
  parseRoomType,
  roomTypeOptions,
  validateCreateRoomForm,
} from "../model/createRoomFormModel";
import { useCreateRoom } from "../model/useCreateRoom";

interface CreateRoomFormProps {
  buildingId: number;
  floors: FloorDto[];
  onCreated?: () => void;
  onCancel?: () => void;
}

export function CreateRoomForm({
  buildingId,
  floors,
  onCreated,
  onCancel,
}: CreateRoomFormProps): JSX.Element {
  const firstFloorId = floors[0]?.id ?? 0;
  const [form, setForm] = useState<CreateRoomRequest>(() =>
    createDefaultRoomForm(firstFloorId),
  );
  const [validationError, setValidationError] = useState<string | null>(null);
  const createRoom = useCreateRoom(buildingId);

  useEffect(() => {
    if (form.floorId === 0 && firstFloorId > 0) {
      setForm((current) => ({ ...current, floorId: firstFloorId }));
    }
  }, [firstFloorId, form.floorId]);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const nextValidationError = validateCreateRoomForm(form);

    if (nextValidationError) {
      setValidationError(nextValidationError);
      return;
    }

    setValidationError(null);
    createRoom.mutate(
      {
        ...form,
        name: form.name.trim(),
      },
      {
        onSuccess: () => {
          setForm(createDefaultRoomForm(firstFloorId));
          onCreated?.();
        },
      },
    );
  };

  return (
    <Stack component="form" spacing={2} onSubmit={handleSubmit}>
      {floors.length === 0 ? (
        <Alert severity="warning">Для помещения нужен этаж. Сначала добавьте этаж здания.</Alert>
      ) : null}
      {(validationError || createRoom.isError) && (
        <Alert severity="error">{validationError ?? getErrorMessage(createRoom.error)}</Alert>
      )}
      <TextField
        label="Название"
        value={form.name}
        required
        autoFocus
        onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
      />
      <TextField
        select
        label="Этаж"
        value={form.floorId || ""}
        required
        onChange={(event) =>
          setForm((current) => ({ ...current, floorId: Number(event.target.value) }))
        }
      >
        {floors.map((floor) => (
          <MenuItem key={floor.id} value={floor.id}>
            {floor.name}
          </MenuItem>
        ))}
      </TextField>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <TextField
          label="Площадь, м²"
          type="number"
          value={form.area || ""}
          required
          inputProps={{ min: 1, step: 0.1 }}
          onChange={(event) =>
            setForm((current) => ({ ...current, area: Number(event.target.value) }))
          }
          fullWidth
        />
        <TextField
          label="Высота, м"
          type="number"
          value={form.height ?? ""}
          inputProps={{ min: 1, step: 0.1 }}
          onChange={(event) =>
            setForm((current) => ({ ...current, height: Number(event.target.value) }))
          }
          fullWidth
        />
      </Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <TextField
          label="Температура внутри, °C"
          type="number"
          value={form.designIndoorTemperature ?? ""}
          inputProps={{ step: 0.5 }}
          onChange={(event) =>
            setForm((current) => ({
              ...current,
              designIndoorTemperature: Number(event.target.value),
            }))
          }
          fullWidth
        />
        <TextField
          label="Людей"
          type="number"
          value={form.peopleCount ?? 0}
          inputProps={{ min: 0, step: 1 }}
          onChange={(event) =>
            setForm((current) => ({ ...current, peopleCount: Number(event.target.value) }))
          }
          fullWidth
        />
      </Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <TextField
          label="Оборудование, Вт"
          type="number"
          value={form.equipmentLoadW ?? 0}
          inputProps={{ min: 0, step: 10 }}
          onChange={(event) =>
            setForm((current) => ({ ...current, equipmentLoadW: Number(event.target.value) }))
          }
          fullWidth
        />
        <TextField
          label="Освещение, Вт"
          type="number"
          value={form.lightingLoadW ?? 0}
          inputProps={{ min: 0, step: 10 }}
          onChange={(event) =>
            setForm((current) => ({ ...current, lightingLoadW: Number(event.target.value) }))
          }
          fullWidth
        />
      </Stack>
      <TextField
        select
        label="Тип помещения"
        value={form.type ?? "Office"}
        onChange={(event) =>
          setForm((current) => ({ ...current, type: parseRoomType(event.target.value) }))
        }
      >
        {roomTypeOptions.map((option) => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </TextField>
      <Stack direction="row" spacing={1} justifyContent="flex-end">
        {onCancel ? (
          <Button type="button" color="inherit" onClick={onCancel}>
            Отмена
          </Button>
        ) : null}
        <Button
          type="submit"
          variant="contained"
          disabled={createRoom.isPending || floors.length === 0}
        >
          Добавить
        </Button>
      </Stack>
    </Stack>
  );
}
