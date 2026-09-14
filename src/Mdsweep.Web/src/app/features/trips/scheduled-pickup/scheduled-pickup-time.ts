/** Parse a wall-clock time without dates, time zones, or browser date parsing. */
export function parseScheduledPickupTime(value: string): string | null {
  let time = value.replace(/[\u061c\u200e\u200f]/g, '').trim();
  const suffix = time.match(/\s*(a\.?\s*m\.?|p\.?\s*m\.?|ص|م)$/i);
  const period = suffix?.[1].replace(/[.\s]/g, '').toUpperCase();
  if (suffix) time = time.slice(0, suffix.index).trim();

  const parts = time.match(/^(\d{1,2})(?::(\d{2}))?$/) ?? time.match(/^(\d{1,2})(\d{2})$/);
  if (!parts) return null;
  let hours = Number(parts[1]);
  const minutes = Number(parts[2] ?? 0);
  if (minutes > 59) return null;
  if (period) {
    if (hours < 1 || hours > 12) return null;
    hours = (hours % 12) + (period === 'PM' || period === 'م' ? 12 : 0);
  } else if (hours > 23) {
    return null;
  }
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:00`;
}
