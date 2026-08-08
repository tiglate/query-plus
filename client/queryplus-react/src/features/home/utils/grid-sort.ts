import type { GridCell } from "@/api/types";

export interface SortState {
    index: number;
    asc: boolean;
}

export function compareGridCells(a: GridCell, b: GridCell): number {
    if (a === b) return 0;
    if (a === null || a === undefined) return -1;
    if (b === null || b === undefined) return 1;
    if (typeof a === "number" && typeof b === "number") return a - b;
    return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: "base" });
}

export function sortRows(rows: GridCell[][], sort: SortState | null): GridCell[][] {
    if (!sort) return rows;
    return [...rows].sort(
        (a, b) =>
            compareGridCells(a[sort.index] ?? null, b[sort.index] ?? null) * (sort.asc ? 1 : -1),
    );
}
