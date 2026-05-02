import { Alert, Button, Stack, TextField } from "@mui/material";
import { FormEvent, useState } from "react";
import type { CreateBuildingRequest } from "@/entities/building/types";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { useCreateBuilding } from "../model/useCreateBuilding";

interface CreateBuildingFormProps {
  projectId: number;
  onCreated?: () => void;
  onCancel?: () => void;
}

export function CreateBuildingForm({
  projectId,
  onCreated,
  onCancel,
}: CreateBuildingFormProps): JSX.Element {
  const [form, setForm] = useState<CreateBuildingRequest>({
    name: "",
    climateZoneId: null,
  });
  const [validationError, setValidationError] = useState<string | null>(null);
  const createBuilding = useCreateBuilding(projectId);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!form.name.trim()) {
      setValidationError("Building name is required.");
      return;
    }

    setValidationError(null);
    createBuilding.mutate(
      {
        ...form,
        name: form.name.trim(),
      },
      {
        onSuccess: () => {
          setForm({ name: "", climateZoneId: null });
          onCreated?.();
        },
      },
    );
  };

  return (
    <Stack component="form" spacing={2} onSubmit={handleSubmit}>
      {(validationError || createBuilding.isError) && (
        <Alert severity="error">
          {validationError ?? getErrorMessage(createBuilding.error)}
        </Alert>
      )}
      <TextField
        label="Building name"
        value={form.name}
        required
        autoFocus
        onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
      />
      <TextField
        label="Climate zone ID"
        helperText="Optional. Use the development demo seed or import climate data before ISO calculations."
        type="number"
        value={form.climateZoneId ?? ""}
        onChange={(event) =>
          setForm((current) => ({
            ...current,
            climateZoneId: event.target.value ? Number(event.target.value) : null,
          }))
        }
      />
      <Stack direction="row" spacing={1} justifyContent="flex-end">
        {onCancel ? (
          <Button type="button" color="inherit" onClick={onCancel}>
            Cancel
          </Button>
        ) : null}
        <Button type="submit" variant="contained" disabled={createBuilding.isPending}>
          Create
        </Button>
      </Stack>
    </Stack>
  );
}
