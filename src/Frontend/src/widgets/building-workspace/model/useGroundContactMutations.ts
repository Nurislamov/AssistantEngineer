import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type { UpsertRoomGroundContactRequest } from "@/entities/room/types";

export function useGroundContactMutations({
  roomId,
  onChanged,
}: {
  roomId: number;
  onChanged: () => void;
}) {
  const [form, setForm] = useState<UpsertRoomGroundContactRequest>(defaultGroundContact());

  const save = useMutation({
    mutationFn: () => roomsApi.upsertGroundContact(roomId, form),
    onSuccess: (data) => {
      setForm(data);
      onChanged();
    },
  });

  const remove = useMutation({
    mutationFn: () => roomsApi.deleteGroundContact(roomId),
    onSuccess: onChanged,
  });

  return {
    form,
    setForm,
    save,
    remove,
  };
}

export function defaultGroundContact(): UpsertRoomGroundContactRequest {
  return {
    contactType: "SlabOnGround",
    exposedPerimeterM: 10,
    burialDepthM: 0,
    wallHeightBelowGradeM: 0,
    horizontalInsulationWidthM: 0,
    perimeterInsulationDepthM: 0,
    underfloorVentilationAirChangesPerHour: 0,
  };
}
