import { useMutation, useQueryClient } from "@tanstack/react-query";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { CreateRoomRequest } from "@/entities/room/types";
import { queryKeys } from "@/shared/api/queryKeys";

export function useCreateRoom(buildingId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateRoomRequest) => roomsApi.create(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.rooms.byBuilding(buildingId) });
    },
  });
}
