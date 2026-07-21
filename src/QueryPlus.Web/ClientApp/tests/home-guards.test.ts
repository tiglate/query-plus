import { describe, expect, it } from "vite-plus/test";
import {
    canExport,
    findMissingRequiredCaptions,
    formatRequiredParamsMessage,
    isParamFieldName,
    isValidProcedureId,
} from "@/pages/home/homeGuards";

describe("homeGuards", () => {
    it("isValidProcedureId", () => {
        expect(isValidProcedureId(null)).toBe(false);
        expect(isValidProcedureId("")).toBe(false);
        expect(isValidProcedureId("0")).toBe(false);
        expect(isValidProcedureId("-1")).toBe(false);
        expect(isValidProcedureId("12")).toBe(true);
        expect(isValidProcedureId("  5  ")).toBe(true);
    });

    it("isParamFieldName", () => {
        expect(isParamFieldName("param_Foo")).toBe(true);
        expect(isParamFieldName("paramcheck_Bar")).toBe(true);
        expect(isParamFieldName("procedureId")).toBe(false);
        expect(isParamFieldName(null)).toBe(false);
    });

    it("formatRequiredParamsMessage", () => {
        expect(formatRequiredParamsMessage(["Name"], "Required: {0}", "Required many: {0}")).toBe(
            "Required: Name",
        );
        expect(formatRequiredParamsMessage(["A", "B"], "Required: {0}", "Required many: {0}")).toBe(
            "Required many: A, B",
        );
    });

    it("findMissingRequiredCaptions marks empty fields", () => {
        const marked: string[] = [];
        const missing = findMissingRequiredCaptions([
            {
                caption: "A",
                value: "",
                markInvalid: () => marked.push("A"),
            },
            {
                caption: "B",
                value: "ok",
                markInvalid: () => marked.push("B"),
            },
            {
                caption: "C",
                value: "   ",
                markInvalid: () => marked.push("C"),
            },
        ]);
        expect(missing).toEqual(["A", "C"]);
        expect(marked).toEqual(["A", "C"]);
    });

    it("canExport requires procedure and export-ready flag", () => {
        expect(canExport(true, true)).toBe(true);
        expect(canExport(true, false)).toBe(false);
        expect(canExport(false, true)).toBe(false);
    });
});
