import SaveIcon from "@mui/icons-material/Save";
import {
  Alert,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type {
  GroundContactTypeDto,
  RoomDto,
  RoomGroundContactDto,
} from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import {
  defaultGroundContact,
  useGroundContactMutations,
} from "../model/useGroundContactMutations";
import { JsonBlock } from "./JsonBlock";
import { RoomSelect } from "./RoomSelect";

const groundContactTypes: GroundContactTypeDto[] = [
  "SlabOnGround",
  "BasementConditioned",
  "BasementUnconditioned",
  "CrawlSpace",
  "VentilatedCrawlSpace",
];

interface GroundContactPanelProps {
  rooms: RoomDto[];
}

export function GroundContactPanel({
  rooms,
}: GroundContactPanelProps): JSX.Element {
  const [roomId, setRoomId] = useState(rooms[0]?.id ?? 0);
  const selectedRoomId = roomId || rooms[0]?.id || 0;
  const query = useQuery({
    queryKey: queryKeys.rooms.groundContact(selectedRoomId),
    queryFn: () => roomsApi.getGroundContact(selectedRoomId),
    enabled: selectedRoomId > 0,
    retry: false,
  });

  const {
    form,
    setForm,
    save,
    remove,
  } = useGroundContactMutations({
    roomId: selectedRoomId,
    onChanged: () => {
      void query.refetch();
    },
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Ground contact</Typography>
        <RoomSelect rooms={rooms} roomId={selectedRoomId} onChange={setRoomId} />
        {(query.error || save.isError || remove.isError) ? (
          <Alert severity="warning">
            {getErrorMessage(query.error ?? save.error ?? remove.error)}
          </Alert>
        ) : null}
        {query.data ? (
          <JsonBlock title="Current ground contact" value={query.data} />
        ) : (
          <EmptyState
            title="No ground contact metadata"
            description="Save metadata for slab, basement, or crawl-space heat transfer."
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
            <FormControl size="small" sx={{ minWidth: 190 }}>
              <InputLabel>Contact type</InputLabel>
              <Select
                label="Contact type"
                value={form.contactType}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    contactType: event.target.value as GroundContactTypeDto,
                  }))
                }
              >
                {groundContactTypes.map((type) => (
                  <MenuItem key={type} value={type}>
                    {type}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            {Object.keys(defaultGroundContact())
              .filter((key) => key !== "contactType")
              .map((key) => (
                <TextField
                  key={key}
                  label={key}
                  type="number"
                  size="small"
                  value={form[key as keyof RoomGroundContactDto]}
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
              color="error"
              variant="outlined"
              disabled={remove.isPending || selectedRoomId <= 0}
              onClick={() => remove.mutate()}
            >
              Delete
            </Button>
          </Stack>
        </Stack>
      </Stack>
    </DataCard>
  );
}
