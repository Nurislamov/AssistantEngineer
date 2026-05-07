import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditIcon from "@mui/icons-material/Edit";
import SaveIcon from "@mui/icons-material/Save";
import {
  Alert,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import type { WindowDto } from "@/entities/room/types";
import { formatNumber } from "@/shared/lib/format";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { envelopeDirections, useEnvelopeMutations } from "../model/useEnvelopeMutations";

interface WindowEditorProps {
  roomId: number;
  items: WindowDto[];
  onChanged: () => void;
  error: unknown;
}

export function WindowEditor({
  roomId,
  items,
  onChanged,
  error,
}: WindowEditorProps): JSX.Element {
  const { editingId, form, setForm, save, remove, beginEdit } = useEnvelopeMutations({
    roomId,
    type: "window",
    onChanged,
  });

  return (
    <DataCard>
      <Stack spacing={2}>
        <Typography variant="h6">Windows</Typography>
        {(error || save.isError || remove.isError) ? (
          <Alert severity="error">{getErrorMessage(error ?? save.error ?? remove.error)}</Alert>
        ) : null}

        <Stack
          component="form"
          spacing={1.5}
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate();
          }}
        >
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            <TextField
              label="Area m2"
              type="number"
              size="small"
              value={form.areaM2}
              onChange={(event) =>
                setForm((current) => ({ ...current, areaM2: Number(event.target.value) }))
              }
            />
            <TextField
              label="U-value"
              type="number"
              size="small"
              value={form.uValue}
              onChange={(event) =>
                setForm((current) => ({ ...current, uValue: Number(event.target.value) }))
              }
            />
            <TextField
              label="SHGC"
              type="number"
              size="small"
              value={form.shgc ?? 0.6}
              onChange={(event) =>
                setForm((current) => ({ ...current, shgc: Number(event.target.value) }))
              }
            />
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Orientation</InputLabel>
              <Select
                label="Orientation"
                value={form.orientation}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    orientation: event.target.value as WindowDto["orientation"],
                  }))
                }
              >
                {envelopeDirections.map((direction) => (
                  <MenuItem key={direction} value={direction}>
                    {direction}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button
              type="submit"
              variant="contained"
              startIcon={<SaveIcon />}
              disabled={save.isPending || roomId <= 0}
            >
              {editingId ? "Save" : "Add"}
            </Button>
          </Stack>
        </Stack>

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Area</TableCell>
                <TableCell>U-value</TableCell>
                <TableCell>Orientation</TableCell>
                <TableCell>SHGC</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.id}</TableCell>
                  <TableCell>{formatNumber(item.areaM2)}</TableCell>
                  <TableCell>{formatNumber(item.uValue, 2)}</TableCell>
                  <TableCell>{item.orientation}</TableCell>
                  <TableCell>{item.shgc}</TableCell>
                  <TableCell align="right">
                    <Button size="small" startIcon={<EditIcon />} onClick={() => beginEdit(item)}>
                      Edit
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      startIcon={<DeleteOutlineIcon />}
                      onClick={() => remove.mutate(item.id)}
                    >
                      Delete
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Stack>
    </DataCard>
  );
}
