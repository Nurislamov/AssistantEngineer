import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { ErrorState } from "./ErrorState";
import { LoadingState } from "./LoadingState";

interface QueryStateProps {
  isLoading: boolean;
  error?: unknown;
  loadingLabel?: string;
  onRetry?: () => void;
}

export function QueryState({
  isLoading,
  error,
  loadingLabel,
  onRetry,
}: QueryStateProps): JSX.Element | null {
  if (isLoading) {
    return <LoadingState label={loadingLabel} />;
  }

  if (error) {
    return <ErrorState message={getErrorMessage(error)} onRetry={onRetry} />;
  }

  return null;
}
