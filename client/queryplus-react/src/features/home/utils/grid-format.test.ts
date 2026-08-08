import { test, expect } from "vitest";
import { formatDateCell, formatGridCell, formatGridRows } from "./grid-format";

test("formatDateCell renders a date-only ISO value without a time part, en locale", () => {
    expect(formatDateCell("2018-03-01T00:00:00", "en")).toBe("03/01/2018");
});

test("formatDateCell renders a date-only ISO value without a time part, pt-BR locale", () => {
    expect(formatDateCell("2018-03-01T00:00:00", "pt-BR")).toBe("01/03/2018");
});

test("formatDateCell keeps the time part for a non-midnight value", () => {
    expect(formatDateCell("2018-03-01T14:30:05", "en")).toBe("03/01/2018 14:30:05");
    expect(formatDateCell("2018-03-01T14:30:05", "pt-BR")).toBe("01/03/2018 14:30:05");
});

test("formatDateCell tolerates fractional seconds and a timezone offset", () => {
    expect(formatDateCell("2018-03-01T14:30:05.1234567", "en")).toBe("03/01/2018 14:30:05");
    expect(formatDateCell("2018-03-01T14:30:05Z", "en")).toBe("03/01/2018 14:30:05");
    expect(formatDateCell("2018-03-01T14:30:05+02:00", "en")).toBe("03/01/2018 14:30:05");
});

test("formatDateCell returns null for a non-ISO string", () => {
    expect(formatDateCell("Acme Corp", "en")).toBeNull();
    expect(formatDateCell("2018-03-01", "en")).toBeNull();
});

test("formatGridCell passes numbers, booleans, and null through untouched", () => {
    expect(formatGridCell(42, "en")).toBe(42);
    expect(formatGridCell(true, "en")).toBe(true);
    expect(formatGridCell(null, "en")).toBeNull();
});

test("formatGridCell reformats a matching string cell and leaves others untouched", () => {
    expect(formatGridCell("2018-03-01T00:00:00", "en")).toBe("03/01/2018");
    expect(formatGridCell("Acme Corp", "en")).toBe("Acme Corp");
});

test("formatGridRows maps every cell in every row", () => {
    const rows = [
        ["Acme", "2018-03-01T00:00:00", 1],
        ["Beta", "2020-11-05T09:15:00", 2],
    ];
    expect(formatGridRows(rows, "en")).toEqual([
        ["Acme", "03/01/2018", 1],
        ["Beta", "11/05/2020 09:15:00", 2],
    ]);
});
