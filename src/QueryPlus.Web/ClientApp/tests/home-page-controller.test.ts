import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { HomePageController } from "@/pages/home/HomePageController";

function buildHomeDom(options?: {
  procedureId?: string;
  requiredEmpty?: boolean;
  exportReady?: boolean;
}) {
  document.body.innerHTML = `
    <div class="qp-home-page"
         data-page="home"
         data-msg-select-procedure="Select a procedure"
         data-msg-export-requires-data="Need data"
         data-msg-required-params="Required: {0}"
         data-msg-required-params-multi="Required many: {0}">
      <button id="btn-execute" type="button"></button>
      <button id="btn-clear" type="button" data-clear-url="/clear"></button>
      <button id="btn-export" type="button"></button>
      <p id="procedure-selection-hint"></p>
      <form id="exec-form">
        <input id="procedureId" class="js-procedure-select" value="${options?.procedureId ?? ""}" />
        <div id="execution-parameters">
          <div class="js-param-field" data-required="true" data-caption="StartDate">
            <input class="js-param-input" name="param_StartDate" value="${options?.requiredEmpty === false ? "2020-01-01" : ""}" />
            <span class="js-param-error hidden"></span>
          </div>
        </div>
      </form>
      <div class="qp-home-columns">
        <button id="btn-toggle-results-max"
                data-label-maximize="Maximize"
                data-label-restore="Restore">
          <i class="js-results-max-icon fa-solid fa-expand"></i>
          <span class="js-results-max-label">Maximize</span>
        </button>
      </div>
      <div id="results-panel">
        <div class="js-results-root" data-export-ready="${options?.exportReady ? "true" : "false"}"></div>
      </div>
      <div id="export-status"></div>
      <button type="button" class="js-procedure-item" data-procedure-id="42">Proc</button>
    </div>
  `;
}

describe("HomePageController", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    sessionStorage.clear();
    vi.spyOn(window, "requestAnimationFrame").mockImplementation((cb) => {
      cb(0);
      return 0;
    });
  });

  afterEach(() => {
    document.body.innerHTML = "";
    vi.restoreAllMocks();
  });

  it("disables actions without a procedure", () => {
    buildHomeDom({ procedureId: "" });
    const c = createTestContainer();
    const home = c.resolve(HomePageController);
    home.mount(document);

    expect(home.hasSelectedProcedure()).toBe(false);
    expect((document.getElementById("btn-execute") as HTMLButtonElement).disabled).toBe(true);
    expect((document.getElementById("btn-export") as HTMLButtonElement).disabled).toBe(true);

    home.dispose();
  });

  it("enables execute when procedure selected; export only when ready", () => {
    buildHomeDom({ procedureId: "7", exportReady: false });
    const c = createTestContainer();
    const home = c.resolve(HomePageController);
    home.mount(document);

    expect(home.hasSelectedProcedure()).toBe(true);
    expect((document.getElementById("btn-execute") as HTMLButtonElement).disabled).toBe(false);
    expect((document.getElementById("btn-export") as HTMLButtonElement).disabled).toBe(true);

    document.querySelector(".js-results-root")!.setAttribute("data-export-ready", "true");
    home.updateHomeActionButtons();
    expect((document.getElementById("btn-export") as HTMLButtonElement).disabled).toBe(false);

    home.dispose();
  });

  it("validateRequiredParameters reports empty required fields", () => {
    buildHomeDom({ procedureId: "1", requiredEmpty: true });
    const c = createTestContainer();
    const home = c.resolve(HomePageController);
    home.mount(document);

    const missing = home.validateRequiredParameters();
    expect(missing).toEqual(["StartDate"]);
    expect(
      document.querySelector(".js-param-input")!.classList.contains("input-validation-error"),
    ).toBe(true);

    home.dispose();
  });

  it("blocks execute HTMX when no procedure selected", () => {
    buildHomeDom({ procedureId: "" });
    const c = createTestContainer();
    const home = c.resolve(HomePageController);
    home.mount(document);

    const btn = document.getElementById("btn-execute")!;
    const event = new CustomEvent("htmx:configRequest", {
      bubbles: true,
      cancelable: true,
      detail: { elt: btn, headers: {} },
    });
    document.body.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(document.getElementById("results-panel")!.textContent).toContain("Select a procedure");

    home.dispose();
  });

  it("selects procedure item and updates hidden id", () => {
    buildHomeDom({ procedureId: "" });
    const c = createTestContainer();
    const home = c.resolve(HomePageController);
    home.mount(document);

    const item = document.querySelector(".js-procedure-item") as HTMLElement;
    item.click();

    expect((document.getElementById("procedureId") as HTMLInputElement).value).toBe("42");
    expect(item.classList.contains("is-selected")).toBe(true);
    expect(home.hasSelectedProcedure()).toBe(true);

    home.dispose();
  });

  it("pager HTMX request overrides pageNumber on form and parameters", () => {
    buildHomeDom({ procedureId: "7", requiredEmpty: false });
    const form = document.getElementById("exec-form")!;
    form.insertAdjacentHTML(
      "beforeend",
      `<input id="pageNumber" name="pageNumber" class="js-page-number" value="1" />
       <button type="button" class="js-results-page" data-page="3" id="page-btn">3</button>`,
    );

    const c = createTestContainer();
    const home = c.resolve(HomePageController);
    home.mount(document);

    const parameters: Record<string, string> = { pageNumber: "1" };
    const btn = document.getElementById("page-btn")!;
    const event = new CustomEvent("htmx:configRequest", {
      bubbles: true,
      cancelable: true,
      detail: { elt: btn, headers: {}, parameters },
    });
    document.body.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(false);
    expect((document.getElementById("pageNumber") as HTMLInputElement).value).toBe("3");
    expect(parameters.pageNumber).toBe("3");

    home.dispose();
  });
});
