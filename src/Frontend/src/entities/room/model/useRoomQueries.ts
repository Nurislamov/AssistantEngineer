import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "@/entities/room/api/roomsApi";
import { queryKeys } from "@/shared/api/queryKeys";

export function useRoom(roomId: number) {
  return useQuery({
    queryKey: queryKeys.rooms.detail(roomId),
    queryFn: () => roomsApi.getById(roomId),
    enabled: Number.isFinite(roomId) && roomId > 0,
  });
}
