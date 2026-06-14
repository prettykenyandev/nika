/** Formats a monetary amount with a friendly symbol for KES, falling back to the code. */
export function formatMoney(amount: number, currency: string): string {
  const symbol = currency === "KES" ? "Ksh" : currency;
  return `${symbol} ${amount.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

/** Formats an ISO date (yyyy-MM-dd) for display. */
export function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
