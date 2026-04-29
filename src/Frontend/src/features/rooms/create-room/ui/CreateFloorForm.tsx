import { Alert, Button, Stack, TextField } from "@mui/material";
import { FormEvent, useState } from "react";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { useCreateFloor } from "../model/useCreateFloor";

interface CreateFloorFormProps {
  buildingId: number;
  onCreated?: () => void;
  onCancel?: () => void;
}

export function CreateFloorForm({
  buildingId,
  onCreated,
  onCancel,
}: CreateFloorFormProps): JSX.Element {
  const [name, setName] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const createFloor = useCreateFloor(buildingId);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!name.trim()) {
      setValidationError("Enter a floor name.");
      return;
    }

    setValidationError(null);
    createFloor.mutate(
      { name: name.trim() },
      {
        onSuccess: () => {
          setName("");
          onCreated?.();
        },
      },
    );
  };

  return (
    <Stack component="form" spacing={2} onSubmit={handleSubmit}>
      {(validationError || createFloor.isError) && (
        <Alert severity="error">{validationError ?? getErrorMessage(createFloor.error)}</Alert>
      )}
      <TextField
        label="Floor name"
        value={name}
        required
        autoFocus
        onChange={(event) => setName(event.target.value)}
      />
      <Stack direction="row" spacing={1} justifyContent="flex-end">
        {onCancel ? (
          <Button type="button" color="inherit" onClick={onCancel}>
            Cancel
          </Button>
        ) : null}
        <Button type="submit" variant="contained" disabled={createFloor.isPending}>
          Create
        </Button>
      </Stack>
    </Stack>
  );
}
