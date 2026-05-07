import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import type { FloorDto } from "@/entities/floor/types";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { RoomDto, RoomTypeDto } from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";

export type RoomForm = {
  name: string;
  floorId: number;
  area: number;
  height: number;
  designIndoorTemperature: number;
  outdoorTemperatureOverride: number | null;
  peopleCount: number;
  equipmentLoadW: number;
  lightingLoadW: number;
  type: RoomTypeDto;
};

export function createRoomForm(floorId: number): RoomForm {
  return {
    name: "",
    floorId,
    area: 20,
    height: 3,
    designIndoorTemperature: 22,
    outdoorTemperatureOverride: null,
    peopleCount: 1,
    equipmentLoadW: 200,
    lightingLoadW: 100,
    type: "Office" as RoomTypeDto,
  };
}

export function toRoomForm(room: RoomDto): RoomForm {
  return {
    name: room.name,
    floorId: room.floorId,
    area: room.area,
    height: room.height ?? 3,
    designIndoorTemperature: room.designIndoorTemperature ?? 22,
    outdoorTemperatureOverride: room.outdoorTemperatureOverride ?? null,
    peopleCount: room.peopleCount,
    equipmentLoadW: room.equipmentLoadW,
    lightingLoadW: room.lightingLoadW,
    type: room.type,
  };
}

export function useFloorsRoomsMutations({
  buildingId,
  floors,
}: {
  buildingId: number;
  floors: FloorDto[];
}) {
  const queryClient = useQueryClient();
  const [floorName, setFloorName] = useState("");
  const [editingFloorId, setEditingFloorId] = useState<number | null>(null);
  const [roomForm, setRoomForm] = useState(() => createRoomForm(floors[0]?.id ?? 0));
  const [editingRoomId, setEditingRoomId] = useState<number | null>(null);

  const effectiveRoomFloorId = roomForm.floorId || floors[0]?.id || 0;

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.floors.byBuilding(buildingId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.rooms.byBuilding(buildingId) }),
    ]);
  };

  const saveFloor = useMutation({
    mutationFn: () =>
      editingFloorId
        ? floorsApi.update(editingFloorId, { name: floorName.trim() })
        : floorsApi.create(buildingId, { name: floorName.trim() }),
    onSuccess: async () => {
      setFloorName("");
      setEditingFloorId(null);
      await invalidate();
    },
  });

  const deleteFloor = useMutation({
    mutationFn: (floorId: number) => floorsApi.delete(floorId),
    onSuccess: invalidate,
  });

  const saveRoom = useMutation({
    mutationFn: () =>
      editingRoomId
        ? roomsApi.update(editingRoomId, roomForm)
        : roomsApi.create({
            ...roomForm,
            floorId: effectiveRoomFloorId,
          }),
    onSuccess: async () => {
      setRoomForm(createRoomForm(floors[0]?.id ?? 0));
      setEditingRoomId(null);
      await invalidate();
    },
  });

  const deleteRoom = useMutation({
    mutationFn: (roomId: number) => roomsApi.delete(roomId),
    onSuccess: invalidate,
  });

  const resetRoomForm = () => setRoomForm(createRoomForm(floors[0]?.id ?? 0));

  return {
    floorName,
    setFloorName,
    editingFloorId,
    setEditingFloorId,
    roomForm,
    setRoomForm,
    editingRoomId,
    setEditingRoomId,
    effectiveRoomFloorId,
    saveFloor,
    deleteFloor,
    saveRoom,
    deleteRoom,
    resetRoomForm,
  };
}
