import type { GridCell } from "@/api/types";

// Matches System.Text.Json's default DateTime serialization (ISO 8601, optional fractional
// seconds/offset). There's no column-level Date-vs-DateTime type reaching the client (see
// GridColumnDto), so a midnight time part is treated as a date-only value - the same heuristic
// most generic grids use when the exact SQL type isn't available.
const ISO_DATETIME_RE =
    /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?$/;

export function formatDateCell(value: string, locale: string): string | null {
    const match = ISO_DATETIME_RE.exec(value);
    if (!match) return null;
    const [, year, month, day, hour, minute, second] = match;
    const datePart = locale.startsWith("pt")
        ? `${day}/${month}/${year}`
        : `${month}/${day}/${year}`;
    const isMidnight = hour === "00" && minute === "00" && second === "00";
    return isMidnight ? datePart : `${datePart} ${hour}:${minute}:${second}`;
}

export function formatGridCell(value: GridCell, locale: string): GridCell {
    if (typeof value !== "string") return value;
    return formatDateCell(value, locale) ?? value;
}

export function formatGridRows(rows: GridCell[][], locale: string): GridCell[][] {
    return rows.map((row) => row.map((cell) => formatGridCell(cell, locale)));
}
