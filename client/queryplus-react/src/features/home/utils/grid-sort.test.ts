import { test, expect } from "vitest";
import { compareGridCells, sortRows } from "./grid-sort";

test("compareGridCells handles nulls, numbers, and strings correctly", () => {
    expect(compareGridCells(null, null)).toBe(0);
    expect(compareGridCells(null, 10)).toBe(-1);
    expect(compareGridCells(10, null)).toBe(1);

    expect(compareGridCells(5, 20)).toBeLessThan(0);
    expect(compareGridCells(20, 5)).toBeGreaterThan(0);
    expect(compareGridCells(15, 15)).toBe(0);

    expect(compareGridCells("Alpha", "Beta")).toBeLessThan(0);
    expect(compareGridCells("Item 2", "Item 10")).toBeLessThan(0);
});

test("sortRows sorts rows by column index ascending and descending", () => {
    const rows = [
        ["Charlie", 30],
        ["Alice", 10],
        ["Bob", 20],
    ];

    const sortedAsc = sortRows(rows, { index: 1, asc: true });
    expect(sortedAsc.map((r) => r[0])).toEqual(["Alice", "Bob", "Charlie"]);

    const sortedDesc = sortRows(rows, { index: 1, asc: false });
    expect(sortedDesc.map((r) => r[0])).toEqual(["Charlie", "Bob", "Alice"]);
});

test("sortRows returns original array if sort is null", () => {
    const rows = [["A"], ["B"]];
    expect(sortRows(rows, null)).toBe(rows);
});
