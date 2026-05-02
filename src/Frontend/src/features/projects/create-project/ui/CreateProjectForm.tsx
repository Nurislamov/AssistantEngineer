import { Alert, Button, Stack, TextField } from "@mui/material";
import { FormEvent, useState } from "react";
import type { ProjectDto } from "@/entities/building/types";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { useCreateProject } from "../model/useCreateProject";

interface CreateProjectFormProps {
  onCreated?: (project: ProjectDto) => void;
  onCancel?: () => void;
}

export function CreateProjectForm({
  onCreated,
  onCancel,
}: CreateProjectFormProps): JSX.Element {
  const [name, setName] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const createProject = useCreateProject();

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (name.trim().length < 2) {
      setValidationError("Project name must contain at least 2 characters.");
      return;
    }

    setValidationError(null);
    createProject.mutate(
      { name: name.trim() },
      {
        onSuccess: (project) => {
          setName("");
          onCreated?.(project);
        },
      },
    );
  };

  return (
    <Stack component="form" spacing={2} onSubmit={handleSubmit}>
      {(validationError || createProject.isError) && (
        <Alert severity="error">{validationError ?? getErrorMessage(createProject.error)}</Alert>
      )}
      <TextField
        label="Project name"
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
        <Button type="submit" variant="contained" disabled={createProject.isPending}>
          Create
        </Button>
      </Stack>
    </Stack>
  );
}
