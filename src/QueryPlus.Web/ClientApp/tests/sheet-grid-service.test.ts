import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { SheetGridService } from "@/components/sheet-grid/SheetGridService";

class FakeClusterize {
  static lastOptions: unknown;
  rows: string[];
  constructor(options: { rows: string[] }) {
    FakeClusterize.lastOptions = options;
    this.rows = options.rows;
  }
  update(rows: string[]) {
    this.rows = rows;
  }
  refresh() {}
  destroy() {}
}

function buildSheetRoot(): HTMLElement {
  const root = document.createElement("div");
  root.className = "js-sheet-root qp-sheet-grid";
  root.innerHTML = `
    <div class="clusterize qp-results-clusterize">
      <div class="clusterize-headers qp-results-headers qp-sheet-headers">
        <table class="qp-sheet js-sheet-headers-table">
          <colgroup class="js-sheet-colgroup-header"></colgroup>
          <thead><tr class="js-sheet-header-row"></tr></thead>
        </table>
      </div>
      <div class="clusterize-scroll qp-results-scroll js-sheet-scroll">
        <table class="qp-sheet js-sheet-body-table">
          <colgroup class="js-sheet-colgroup-body"></colgroup>
          <tbody class="clusterize-content js-sheet-content"></tbody>
        </table>
      </div>
    </div>
    <script type="application/json" class="js-sheet-data">{"columns":[{"caption":"Id","align":"left"},{"caption":"Name","align":"left"}],"cells":[["1","Alpha"],["2","Beta"],["3","Gamma"]]}</script>
  `;
  document.body.appendChild(root);
  return root;
}

describe("SheetGridService", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    (window as Window & { Clusterize?: typeof FakeClusterize }).Clusterize =
      FakeClusterize as unknown as typeof Clusterize;
    // jsdom has no canvas implementation; auto-size falls back to min width.
    vi.spyOn(HTMLCanvasElement.prototype, "getContext").mockReturnValue(null);
    vi.spyOn(window, "requestAnimationFrame").mockImplementation((cb) => {
      cb(0);
      return 0;
    });
  });

  afterEach(() => {
    document.body.innerHTML = "";
    delete (window as Window & { Clusterize?: unknown }).Clusterize;
    delete (window as Window & { QueryPlusSheetGrid?: unknown }).QueryPlusSheetGrid;
    vi.restoreAllMocks();
  });

  it("mounts with Clusterize and installs global bridge", () => {
    const c = createTestContainer();
    const service = c.resolve(SheetGridService);
    service.installGlobalBridge();

    expect(window.QueryPlusSheetGrid).toBeDefined();

    const root = buildSheetRoot();
    const state = service.mount(root);

    expect(state).not.toBeNull();
    expect(state?.columns).toHaveLength(2);
    expect(state?.cells).toHaveLength(3);
    expect(FakeClusterize.lastOptions).toMatchObject({
      tag: "tr",
      rows_in_block: 50,
    });
    expect(root.querySelectorAll(".qp-sheet-th").length).toBe(2);

    service.destroy(root);
    expect(service.getState(root)).toBeNull();
  });

  it("returns null when Clusterize is missing", () => {
    delete (window as Window & { Clusterize?: unknown }).Clusterize;
    const c = createTestContainer();
    const service = c.resolve(SheetGridService);
    const root = buildSheetRoot();
    expect(service.mount(root)).toBeNull();
  });
});
