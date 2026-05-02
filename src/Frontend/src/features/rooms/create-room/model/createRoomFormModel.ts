import type { CreateRoomRequest, RoomTypeDto } from "@/entities/room/types";

export const roomTypeOptions: Array<{ value: RoomTypeDto; label: string }> = [
  { value: "Office", label: "Office" },
  { value: "MeetingRoom", label: "Meeting room" },
  { value: "Corridor", label: "Corridor" },
  { value: "ServerRoom", label: "Server room" },
  { value: "Retail", label: "Retail" },
  { value: "Residential", label: "Residential" },
  { value: "Other", label: "Other" },
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
    return "Room name must be at least 2 characters.";
  }

  if (form.floorId <= 0) {
    return "Select a floor.";
  }

  if (form.area < 1) {
    return "Room area must be at least 1 m2.";
  }

  if ((form.height ?? 0) < 1) {
    return "Room height must be at least 1 m.";
  }

  return null;
}
