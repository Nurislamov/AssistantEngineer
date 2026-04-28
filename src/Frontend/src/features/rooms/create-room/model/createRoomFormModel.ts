import type { CreateRoomRequest, RoomTypeDto } from "@/entities/room/types";

export const roomTypeOptions: Array<{ value: RoomTypeDto; label: string }> = [
  { value: "Office", label: "Офис" },
  { value: "MeetingRoom", label: "Переговорная" },
  { value: "Corridor", label: "Коридор" },
  { value: "ServerRoom", label: "Серверная" },
  { value: "Retail", label: "Торговое" },
  { value: "Residential", label: "Жилое" },
  { value: "Other", label: "Другое" },
];

export function createDefaultRoomForm(floorId: number): CreateRoomRequest {
  return {
    name: "",
    floorId,
    area: 0,
    height: 3,
    designIndoorTemperature: 22,
    peopleCount: 0,
    equipmentLoadW: 0,
    lightingLoadW: 0,
    type: "Office",
  };
}

function isRoomType(value: string): value is RoomTypeDto {
  return roomTypeOptions.some((option) => option.value === value);
}

export function parseRoomType(value: string): RoomTypeDto {
  return isRoomType(value) ? value : "Office";
}

export function validateCreateRoomForm(form: CreateRoomRequest): string | null {
  if (form.name.trim().length < 2) {
    return "Название помещения должно быть не короче 2 символов";
  }

  if (form.floorId <= 0) {
    return "Выберите этаж";
  }

  if (form.area < 1) {
    return "Площадь должна быть не меньше 1 м²";
  }

  if ((form.height ?? 0) < 1) {
    return "Высота должна быть не меньше 1 м";
  }

  return null;
}
