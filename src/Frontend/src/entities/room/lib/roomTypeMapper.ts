import type { RoomTypeApiValue, RoomTypeDto } from "../types";

const roomTypeByCode: Record<number, RoomTypeDto> = {
  0: "Office",
  1: "MeetingRoom",
  2: "Corridor",
  3: "ServerRoom",
  4: "Retail",
  5: "Residential",
  6: "Other",
};

const roomTypeCodeByValue: Record<RoomTypeDto, number> = {
  Office: 0,
  MeetingRoom: 1,
  Corridor: 2,
  ServerRoom: 3,
  Retail: 4,
  Residential: 5,
  Other: 6,
};

function isRoomType(value: string): value is RoomTypeDto {
  return Object.prototype.hasOwnProperty.call(roomTypeCodeByValue, value);
}

export function toRoomTypeDto(value: RoomTypeApiValue): RoomTypeDto {
  if (typeof value === "number") {
    return roomTypeByCode[value] ?? "Other";
  }

  return isRoomType(value) ? value : "Other";
}

export function toRoomTypeApiValue(value: RoomTypeDto): number {
  return roomTypeCodeByValue[value];
}
