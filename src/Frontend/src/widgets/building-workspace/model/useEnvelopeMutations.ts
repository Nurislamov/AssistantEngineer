import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type {
  CardinalDirectionDto,
  UpsertWallRequest,
  UpsertWindowRequest,
  WallBoundaryTypeDto,
  WallDto,
  WindowDto,
} from "@/entities/room/types";

export const envelopeDirections: CardinalDirectionDto[] = [
  "North",
  "NorthEast",
  "East",
  "SouthEast",
  "South",
  "SouthWest",
  "West",
  "NorthWest",
];

export const envelopeWallBoundaryTypes: WallBoundaryTypeDto[] = [
  "External",
  "Ground",
  "Adiabatic",
  "AdjacentConditioned",
  "AdjacentUnconditioned",
];

export type EnvelopeForm = UpsertWallRequest & Partial<UpsertWindowRequest>;
export type EnvelopeMutationType = "wall" | "window";

export function useEnvelopeMutations({
  roomId,
  type,
  onChanged,
}: {
  roomId: number;
  type: EnvelopeMutationType;
  onChanged: () => void;
}) {
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<EnvelopeForm>(() => defaultEnvelopeForm());

  const save = useMutation<WallDto | WindowDto, Error, void>({
    mutationFn: () => {
      if (type === "wall") {
        const request: UpsertWallRequest = {
          areaM2: Number(form.areaM2),
          uValue: Number(form.uValue),
          orientation: form.orientation,
          boundaryType: form.boundaryType,
          adjacentRoomId: form.adjacentRoomId ?? null,
        };

        return editingId
          ? roomsApi.updateWall(roomId, editingId, request)
          : roomsApi.createWall(roomId, request);
      }

      const request: UpsertWindowRequest = {
        areaM2: Number(form.areaM2),
        uValue: Number(form.uValue),
        shgc: Number(form.shgc ?? 0.6),
        orientation: form.orientation,
        shading: form.shading ?? defaultWindowShading(),
      };

      return editingId
        ? roomsApi.updateWindow(roomId, editingId, request)
        : roomsApi.createWindow(roomId, request);
    },
    onSuccess: () => {
      setEditingId(null);
      setForm(defaultEnvelopeForm());
      onChanged();
    },
  });

  const remove = useMutation({
    mutationFn: (id: number) =>
      type === "wall"
        ? roomsApi.deleteWall(roomId, id)
        : roomsApi.deleteWindow(roomId, id),
    onSuccess: onChanged,
  });

  const beginEdit = (item: WallDto | WindowDto) => {
    setEditingId(item.id);

    if ("boundaryType" in item) {
      setForm({
        areaM2: item.areaM2,
        uValue: item.uValue,
        orientation: item.orientation,
        boundaryType: item.boundaryType,
        adjacentRoomId: item.adjacentRoomId ?? null,
      });
      return;
    }

    setForm({
      ...defaultEnvelopeForm(),
      areaM2: item.areaM2,
      uValue: item.uValue,
      orientation: item.orientation,
      shgc: item.shgc,
      shading: item.shading,
    });
  };

  return {
    editingId,
    form,
    setForm,
    save,
    remove,
    beginEdit,
  };
}

function defaultWindowShading() {
  return {
    overhangDepthM: 0,
    sideFinDepthM: 0,
    revealDepthM: 0,
    windowHeightM: 0,
    windowWidthM: 0,
    minimumDirectSolarReductionFactor: 0.15,
    diffuseSolarShareUnaffected: 0.3,
  };
}

function defaultEnvelopeForm(): EnvelopeForm {
  return {
    areaM2: 10,
    uValue: 1.2,
    orientation: "South",
    boundaryType: "External",
    adjacentRoomId: null,
    shgc: 0.55,
    shading: defaultWindowShading(),
  };
}
