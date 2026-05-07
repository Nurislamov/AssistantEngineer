import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { roomsApi } from "@/entities/room/api/roomsApi";
import type {
  NaturalVentilationPreviewDto,
  RoomVentilationParametersDto,
  UpsertRoomVentilationParametersRequest,
} from "@/entities/room/types";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";

export function useVentilationMutations({
  roomId,
  onChanged,
}: {
  roomId: number;
  onChanged: () => void;
}) {
  const [form, setForm] = useState<UpsertRoomVentilationParametersRequest>(defaultVentilation());
  const [preview, setPreview] = useState<NaturalVentilationPreviewDto | RoomVentilationParametersDto | null>(null);
  const [operationError, setOperationError] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () => roomsApi.upsertVentilation(roomId, form),
    onSuccess: (data) => {
      setForm(data);
      setOperationError(null);
      onChanged();
    },
  });

  const remove = useMutation({
    mutationFn: () => roomsApi.deleteVentilation(roomId),
    onSuccess: () => {
      setOperationError(null);
      onChanged();
    },
  });

  const runVentilationOperation = async (
    operation: () => Promise<NaturalVentilationPreviewDto | RoomVentilationParametersDto>,
    onSuccess: (result: NaturalVentilationPreviewDto | RoomVentilationParametersDto) => void,
  ) => {
    setOperationError(null);
    try {
      onSuccess(await operation());
    } catch (error) {
      setOperationError(getErrorMessage(error));
    }
  };

  const previewDefaults = () =>
    runVentilationOperation(
      () => roomsApi.previewVentilationDefaults(roomId),
      setPreview,
    );

  const applyDefaults = () =>
    runVentilationOperation(
      () => roomsApi.applyVentilationDefaults(roomId, true),
      (result) => {
        setForm(result as RoomVentilationParametersDto);
        onChanged();
      },
    );

  const previewNaturalVentilation = () =>
    runVentilationOperation(
      () =>
        roomsApi.previewNaturalVentilation(roomId, {
          indoorTemperatureC: 24,
          outdoorTemperatureC: 18,
          windSpeedMPerS: 2,
          demandFactor: 0.8,
          hourOfDay: 14,
        }),
      setPreview,
    );

  return {
    form,
    setForm,
    preview,
    operationError,
    save,
    remove,
    previewDefaults,
    applyDefaults,
    previewNaturalVentilation,
  };
}

function defaultVentilation(): UpsertRoomVentilationParametersRequest {
  return {
    airChangesPerHour: 1,
    heatRecoveryEfficiency: 0,
    infiltrationAirChangesPerHour: 0.2,
    windExposureFactor: 1,
    stackCoefficient: 0.04,
    windCoefficient: 0.12,
  };
}
