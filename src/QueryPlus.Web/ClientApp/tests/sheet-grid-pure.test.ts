import { describe, expect, it } from "vite-plus/test";
import { COL_MIN } from "../src/components/sheet-grid/constants";
import { escapeHtml } from "../src/components/sheet-grid/dom";
import { totalColumnsWidth } from "../src/components/sheet-grid/measure";
import { buildRowHtml } from "../src/components/sheet-grid/render";
import {
  applyColumnReorder,
  compareSortValues,
} from "../src/components/sheet-grid/sort";
import type { SheetColumn } from "../src/components/sheet-grid/types";

describe("sheet-grid pure helpers", () => {
  it("escapeHtml encodes markup", () => {
    expect(escapeHtml(`a <b> & "c"`)).toBe(
      "a &lt;b&gt; &amp; &quot;c&quot;",
    );
  });

  it("compareSortValues sorts numbers and strings", () => {
    expect(compareSortValues("10", "2", true)).toBeGreaterThan(0);
    expect(compareSortValues("10", "2", false)).toBeLessThan(0);
    expect(compareSortValues("apple", "banana", true)).toBeLessThan(0);
    expect(compareSortValues("<b>z</b>", "a", true)).toBeGreaterThan(0);
  });

  it("applyColumnReorder moves columns/cells and tracks sortCol", () => {
    const columns = ["A", "B", "C"];
    const cells = [
      ["a1", "b1", "c1"],
      ["a2", "b2", "c2"],
    ];
    const sortCol = applyColumnReorder(columns, cells, 0, 2, 0);
    expect(columns).toEqual(["B", "C", "A"]);
    expect(cells[0]).toEqual(["b1", "c1", "a1"]);
    expect(sortCol).toBe(2);
  });

  it("buildRowHtml escapes non-html columns", () => {
    const columns: SheetColumn[] = [
      {
        caption: "Name",
        align: "left",
        html: false,
        sortable: true,
        width: 100,
        fixedWidth: false,
      },
      {
        caption: "Actions",
        align: "center",
        html: true,
        sortable: false,
        width: 80,
        fixedWidth: true,
      },
    ];
    const html = buildRowHtml(["<x>", "<button>x</button>"], columns);
    expect(html).toContain("&lt;x&gt;");
    expect(html).toContain("<button>x</button>");
    expect(html).toContain('text-align:center');
  });

  it("totalColumnsWidth sums widths with fallback", () => {
    const columns: SheetColumn[] = [
      {
        caption: "A",
        align: "left",
        html: false,
        sortable: true,
        width: 100,
        fixedWidth: false,
      },
      {
        caption: "B",
        align: "left",
        html: false,
        sortable: true,
        width: 0,
        fixedWidth: false,
      },
    ];
    // width 0 is falsy → COL_MIN
    expect(totalColumnsWidth(columns)).toBe(100 + COL_MIN);
  });
});
