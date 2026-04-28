import type {
  BuildingCoolingLoadApiResponse,
  BuildingHeatingLoadApiResponse,
  CalculationResultDto,
  RoomCoolingLoadApiResponse,
  RoomHeatingLoadApiResponse,
} from "../types";

function createCalculationId(): number {
  return Date.now();
}

export function mapBuildingCalculationResult(
  cooling: BuildingCoolingLoadApiResponse,
  heating: BuildingHeatingLoadApiResponse,
): CalculationResultDto {
  return {
    id: createCalculationId(),
    buildingId: heating.buildingId,
    buildingName: heating.buildingName || cooling.buildingName,
    totalHeatLoss: heating.totalDesignHeatingLoadW,
    totalHeatGain: cooling.totalHeatLoadW,
    coolingLoad: cooling.designCapacityW || cooling.totalHeatLoadW,
    heatingLoad: heating.totalDesignHeatingLoadW,
    calculatedAt: new Date().toISOString(),
    rooms: heating.rooms.map((room) => ({
      roomId: room.roomId,
      roomName: room.roomName,
      heatLoss: room.totalDesignHeatingLoadW,
      heatingLoad: room.totalDesignHeatingLoadW,
    })),
  };
}

export function mapRoomCalculationResult(
  cooling: RoomCoolingLoadApiResponse,
  heating: RoomHeatingLoadApiResponse,
): CalculationResultDto {
  return {
    id: createCalculationId(),
    roomId: heating.roomId,
    roomName: heating.roomName || cooling.roomName,
    totalHeatLoss: heating.totalDesignHeatingLoadW,
    totalHeatGain: cooling.totalHeatLoadW,
    coolingLoad: cooling.designCapacityW || cooling.totalHeatLoadW,
    heatingLoad: heating.totalDesignHeatingLoadW,
    calculatedAt: new Date().toISOString(),
    rooms: [
      {
        roomId: heating.roomId,
        roomName: heating.roomName || cooling.roomName,
        heatLoss: heating.totalDesignHeatingLoadW,
        heatGain: cooling.totalHeatLoadW,
        coolingLoad: cooling.designCapacityW || cooling.totalHeatLoadW,
        heatingLoad: heating.totalDesignHeatingLoadW,
      },
    ],
  };
}
