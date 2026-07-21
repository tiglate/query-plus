import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { ResultsMaximize } from "@/pages/home/ResultsMaximize";

describe("ResultsMaximize", () => {
  beforeEach(() => {
    sessionStorage.clear();
    document.body.innerHTML = `
      <div class="qp-home-columns">
        <section class="qp-home-column-side">Side</section>
        <section class="qp-home-column-results">
          <button type="button" id="btn-toggle-results-max"
                  data-label-maximize="Maximize"
                  data-label-restore="Restore">
            <i class="js-results-max-icon fa-solid fa-expand"></i>
            <span class="js-results-max-label">Maximize</span>
          </button>
        </section>
      </div>
    `;
    vi.spyOn(window, "requestAnimationFrame").mockImplementation((cb) => {
      cb(0);
      return 0;
    });
  });

  afterEach(() => {
    document.body.innerHTML = "";
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it("toggles maximized class when the button (or icon) is clicked", () => {
    const c = createTestContainer();
    const maximize = c.resolve(ResultsMaximize);
    maximize.mount();

    const columns = document.querySelector(".qp-home-columns")!;
    expect(columns.classList.contains("is-results-maximized")).toBe(false);

    document
      .querySelector(".js-results-max-icon")!
      .dispatchEvent(new MouseEvent("click", { bubbles: true }));

    expect(columns.classList.contains("is-results-maximized")).toBe(true);
    expect(document.getElementById("btn-toggle-results-max")!.getAttribute("aria-pressed")).toBe(
      "true",
    );
    expect(document.querySelector(".js-results-max-label")!.textContent).toBe("Restore");

    document.getElementById("btn-toggle-results-max")!.click();
    expect(columns.classList.contains("is-results-maximized")).toBe(false);

    maximize.dispose();
  });
});
