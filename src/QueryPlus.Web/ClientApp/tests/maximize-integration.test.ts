import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { bootstrap, resolvePageKey } from "@/core/bootstrap";
import { getAppContainer, resetContainerConfiguration } from "@/core/di/container";
import { HomePageController } from "@/pages/home/HomePageController";
import { ResultsMaximize } from "@/pages/home/ResultsMaximize";
import { SharedShellController } from "@/pages/shared/SharedShellController";

/**
 * Full home DOM + bootstrap: Maximize must toggle once per click.
 * (Regression: double module evaluation stacked two handlers → no-op.)
 */
describe("Maximize integration via bootstrap", () => {
    beforeEach(() => {
        resetContainerConfiguration();
        sessionStorage.clear();
        document.head.innerHTML = '<meta name="qp-page" content="home" />';
        document.documentElement.removeAttribute("data-qp-client");
        document.body.setAttribute("data-page", "home");
        document.body.innerHTML = `
      <div class="qp-page qp-home-page" data-page="home">
        <form id="exec-form" method="post" class="js-exec-form qp-home-workspace">
          <input type="hidden" id="procedureId" name="procedureId" class="js-procedure-select" value="" />
          <div class="qp-home-columns">
            <section class="qp-card qp-home-column qp-home-column-side">
              <button type="button" class="js-procedure-item" data-procedure-id="1">Proc</button>
            </section>
            <section class="qp-card qp-home-column qp-home-column-side">Params</section>
            <section class="qp-card qp-home-column qp-home-column-results">
              <button type="button" id="btn-toggle-results-max"
                      data-label-maximize="Maximize"
                      data-label-restore="Restore"
                      aria-pressed="false">
                <i class="js-results-max-icon fa-solid fa-expand"></i>
                <span class="js-results-max-label">Maximize</span>
              </button>
              <div id="results-panel">
                <div class="js-results-root" data-export-ready="false"></div>
              </div>
            </section>
          </div>
        </form>
      </div>
    `;
        (window as unknown as { htmx: object }).htmx = {
            on: () => () => {},
            defineExtension: () => {},
            config: {},
        };
    });

    afterEach(() => {
        // Dispose listeners before wiping the DOM / resetting DI.
        try {
            const c = getAppContainer();
            c.resolve(ResultsMaximize).dispose();
            c.resolve(HomePageController).unmount();
            c.resolve(SharedShellController).unmount();
        } catch {
            // container may already be reset
        }
        document.body.innerHTML = "";
        document.head.innerHTML = "";
        document.body.removeAttribute("data-page");
        sessionStorage.clear();
        resetContainerConfiguration();
        vi.restoreAllMocks();
    });

    it("bootstrap mounts maximize and click toggles layout once", () => {
        expect(resolvePageKey(document)).toBe("home");
        const result = bootstrap({ document });
        expect(result.pageKey).toBe("home");
        expect(result.page).not.toBeNull();

        const columns = document.querySelector(".qp-home-columns")!;
        expect(columns.classList.contains("is-results-maximized")).toBe(false);

        document.getElementById("btn-toggle-results-max")!.click();
        expect(columns.classList.contains("is-results-maximized")).toBe(true);
        expect(document.querySelector(".js-results-max-label")!.textContent).toBe("Restore");

        document
            .querySelector(".js-results-max-icon")!
            .dispatchEvent(new MouseEvent("click", { bubbles: true }));
        expect(columns.classList.contains("is-results-maximized")).toBe(false);
    });

    it("HomePageController remount remounts maximize without stacking", () => {
        const result = bootstrap({ document });
        const home = result.page as HomePageController;
        home.mount(document);
        home.mount(document);

        const columns = document.querySelector(".qp-home-columns")!;
        document.getElementById("btn-toggle-results-max")!.click();
        expect(columns.classList.contains("is-results-maximized")).toBe(true);
        document.getElementById("btn-toggle-results-max")!.click();
        expect(columns.classList.contains("is-results-maximized")).toBe(false);
    });
});
