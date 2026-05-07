import SaveIcon from "@mui/icons-material/Save";
import { Alert, Button, Stack, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { RoomDto, UpsertRoomVentilationParametersRequest } from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { useVentilationMutations } from "../model/useVentilationMutations";
import { JsonBlock } from "./JsonBlock";
import { RoomSelect } from "./RoomSelect";

interface VentilationPanelProps {
  rooms: RoomDto[];
}

export function VentilationPanel({ rooms }: VentilationPanelProps): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;

  const query = useQuery({
    queryKey: queryKeys.rooms.ventilation(selectedRoomId),
    queryFn: () => roomsApi.getVentilation(selectedRoomId),
    enabled: selectedRoomId > 0,
    retry: false,
  });

  const {
    form,
    setForm,
    preview,
    operationError,
    save,
    remove,
    previewDefaults,
    applyDefaults,
    previewNaturalVentilation,
  } = useVentilationMutations({
    roomId: selectedRoomId,
    onChanged: () => {
      void query.refetch();
    },
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Ventilation</Typography>
        <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
        {(query.error || save.isError || remove.isError || operationError) ? (
          <Alert severity="warning">
            {operationError ?? getErrorMessage(query.error ?? save.error ?? remove.error)}
          </Alert>
        ) : null}
        {query.data ? (
          <JsonBlock title="Current parameters" value={query.data} />
        ) : (
          <EmptyState
            title="No ventilation parameters"
            description="Save parameters or apply defaults for the selected room."
          />
        )}
        <Stack
          component="form"
          spacing={1}
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate();
          }}
        >
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            {Object.keys(form).map((key) => (
              <TextField
                key={key}
                label={key}
                type="number"
                size="small"
                value={form[key as keyof UpsertRoomVentilationParametersRequest]}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    [key]: Number(event.target.value),
                  }))
                }
              />
            ))}
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button
              type="submit"
              variant="contained"
              startIcon={<SaveIcon />}
              disabled={save.isPending || selectedRoomId <= 0}
            >
              Save
            </Button>
            <Button
              variant="outlined"
              disabled={selectedRoomId <= 0}
              onClick={() => void previewDefaults()}
            >
              Preview defaults
            </Button>
            <Button
              variant="outlined"
              disabled={selectedRoomId <= 0}
              onClick={() => void applyDefaults()}
            >
              Apply defaults
            </Button>
            <Button
              variant="outlined"
              disabled={selectedRoomId <= 0}
              onClick={() => void previewNaturalVentilation()}
            >
              Natural preview
            </Button>
            <Button
              color="error"
              variant="outlined"
              disabled={remove.isPending || selectedRoomId <= 0}
              onClick={() => remove.mutate()}
            >
              Delete
            </Button>
          </Stack>
        </Stack>
        {preview ? <JsonBlock title="Preview" value={preview} /> : null}
      </Stack>
    </DataCard>
  );
}
