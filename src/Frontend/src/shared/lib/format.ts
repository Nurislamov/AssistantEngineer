export function formatNumber(value: number | null | undefined, fractionDigits = 1): string {
  if (value === undefined || value === null || Number.isNaN(value)) {
    return "-";
  }

  return new Intl.NumberFormat("ru-RU", {
    maximumFractionDigits: fractionDigits,
  }).format(value);
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}
