import { test, expect } from "vitest";
import {
    cellText,
    approxWidth,
    clampWidth,
    MIN_COLUMN_WIDTH,
    MAX_COLUMN_WIDTH,
} from "./useColumnWidths";

test("cellText converts null and undefined to empty string", () => {
    expect(cellText(null)).toBe("");
    expect(cellText(undefined)).toBe("");
    expect(cellText("hello")).toBe("hello");
    expect(cellText(123)).toBe("123");
});

test("approxWidth computes text length approximation", () => {
    expect(approxWidth("abc")).toBe(3 * 7.5);
});

test("clampWidth bounds value between min and max", () => {
    expect(clampWidth(10)).toBe(MIN_COLUMN_WIDTH);
    expect(clampWidth(1000)).toBe(MAX_COLUMN_WIDTH);
    expect(clampWidth(150)).toBe(150);
});
