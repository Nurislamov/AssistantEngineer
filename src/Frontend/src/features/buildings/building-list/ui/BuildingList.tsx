import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { paths } from "@/app/router/paths";
import type { BuildingDto } from "@/entities/building/types";
import { EmptyState } from "@/shared/ui/EmptyState";

interface BuildingListProps {
  buildings: BuildingDto[];
}

export function BuildingList({ buildings }: BuildingListProps): JSX.Element {
  if (buildings.length === 0) {
    return (
      <EmptyState
        title="Зданий пока нет"
        description="Создайте первое здание в текущем проекте."
      />
    );
  }

  return (
    <TableContainer>
      <Table size="medium">
        <TableHead>
          <TableRow>
            <TableCell>ID</TableCell>
            <TableCell>Название</TableCell>
            <TableCell>Климатическая зона</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {buildings.map((building) => (
            <TableRow key={building.id} hover>
              <TableCell width={90}>{building.id}</TableCell>
              <TableCell>{building.name}</TableCell>
              <TableCell>{building.climateZoneName ?? building.climateZoneId ?? "-"}</TableCell>
              <TableCell align="right">
                <Button
                  component={RouterLink}
                  to={paths.buildingDetails(building.id)}
                  size="small"
                  endIcon={<OpenInNewIcon />}
                >
                  Открыть
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
