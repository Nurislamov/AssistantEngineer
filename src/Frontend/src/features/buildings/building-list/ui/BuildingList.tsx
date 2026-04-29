import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditIcon from "@mui/icons-material/Edit";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Button,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { paths } from "@/app/router/paths";
import type { BuildingDto } from "@/entities/building/types";
import { EmptyState } from "@/shared/ui/EmptyState";

interface BuildingListProps {
  buildings: BuildingDto[];
  onEdit?: (building: BuildingDto) => void;
  onDelete?: (building: BuildingDto) => void;
}

export function BuildingList({ buildings, onEdit, onDelete }: BuildingListProps): JSX.Element {
  if (buildings.length === 0) {
    return (
      <EmptyState
        title="No buildings yet"
        description="Create the first building in the selected project."
      />
    );
  }

  return (
    <TableContainer>
      <Table size="medium">
        <TableHead>
          <TableRow>
            <TableCell>ID</TableCell>
            <TableCell>Name</TableCell>
            <TableCell>Climate zone</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {buildings.map((building) => (
            <TableRow key={building.id} hover>
              <TableCell width={90}>{building.id}</TableCell>
              <TableCell>{building.name}</TableCell>
              <TableCell>{building.climateZoneName ?? building.climateZoneId ?? "-"}</TableCell>
              <TableCell align="right">
                <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                  {onEdit ? (
                    <Tooltip title="Edit building">
                      <IconButton size="small" onClick={() => onEdit(building)}>
                        <EditIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  ) : null}
                  {onDelete ? (
                    <Tooltip title="Delete building">
                      <IconButton size="small" color="error" onClick={() => onDelete(building)}>
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  ) : null}
                  <Button
                    component={RouterLink}
                    to={paths.buildingDetails(building.id)}
                    size="small"
                    endIcon={<OpenInNewIcon />}
                  >
                    Open
                  </Button>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
