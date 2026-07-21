import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import { NavDropdown } from "@/components/nav-dropdown/NavDropdown";

function buildDropdownDom() {
  const root = document.createElement("div");
  root.setAttribute("data-nav-dropdown", "");
  root.className = "qp-nav-dropdown";

  const trigger = document.createElement("button");
  trigger.type = "button";
  trigger.setAttribute("data-nav-dropdown-trigger", "");
  trigger.setAttribute("aria-expanded", "false");

  const panel = document.createElement("div");
  panel.setAttribute("data-nav-dropdown-panel", "");
  panel.hidden = true;

  root.append(trigger, panel);
  document.body.appendChild(root);
  return { root, trigger, panel };
}

describe("NavDropdown", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    document.body.innerHTML = "";
  });

  afterEach(() => {
    vi.useRealTimers();
    document.body.innerHTML = "";
  });

  it("opens on mouseenter and closes after leave delay", () => {
    const { root, panel, trigger } = buildDropdownDom();
    const dropdown = new NavDropdown(root, trigger, panel, {
      closeDelayMs: 250,
    });
    dropdown.mount();

    root.dispatchEvent(new Event("mouseenter"));
    expect(dropdown.isOpen()).toBe(true);
    expect(panel.hidden).toBe(false);
    expect(trigger.getAttribute("aria-expanded")).toBe("true");
    expect(root.classList.contains("is-open")).toBe(true);

    root.dispatchEvent(new Event("mouseleave"));
    expect(dropdown.isOpen()).toBe(true);

    vi.advanceTimersByTime(249);
    expect(dropdown.isOpen()).toBe(true);

    vi.advanceTimersByTime(1);
    expect(dropdown.isOpen()).toBe(false);
    expect(panel.hidden).toBe(true);
    expect(trigger.getAttribute("aria-expanded")).toBe("false");

    dropdown.dispose();
  });

  it("cancels scheduled close when re-entering before delay", () => {
    const { root, panel, trigger } = buildDropdownDom();
    const dropdown = new NavDropdown(root, trigger, panel, {
      closeDelayMs: 250,
    });
    dropdown.mount();

    root.dispatchEvent(new Event("mouseenter"));
    root.dispatchEvent(new Event("mouseleave"));
    vi.advanceTimersByTime(100);
    root.dispatchEvent(new Event("mouseenter"));
    vi.advanceTimersByTime(250);
    expect(dropdown.isOpen()).toBe(true);

    dropdown.dispose();
  });

  it("toggles on trigger click", () => {
    const { root, panel, trigger } = buildDropdownDom();
    const dropdown = new NavDropdown(root, trigger, panel);
    dropdown.mount();

    trigger.click();
    expect(dropdown.isOpen()).toBe(true);
    trigger.click();
    expect(dropdown.isOpen()).toBe(false);

    dropdown.dispose();
  });

  it("closes on Escape and focuses trigger without re-opening", () => {
    const { root, panel, trigger } = buildDropdownDom();
    const dropdown = new NavDropdown(root, trigger, panel);
    dropdown.mount();
    dropdown.open();
    expect(dropdown.isOpen()).toBe(true);

    // Real focus() fires focusin on root; suppressOpen must keep the panel closed.
    const focusSpy = vi.spyOn(trigger, "focus").mockImplementation(function (this: HTMLElement) {
      this.dispatchEvent(new FocusEvent("focusin", { bubbles: true }));
    });

    dropdown.handleDocumentKeydown({ key: "Escape" });

    expect(panel.hidden).toBe(true);
    expect(dropdown.isOpen()).toBe(false);
    expect(focusSpy).toHaveBeenCalled();

    dropdown.dispose();
  });

  it("close() hides the panel", () => {
    const { root, trigger, panel } = buildDropdownDom();
    const dropdown = new NavDropdown(root, trigger, panel);
    dropdown.open();
    dropdown.close();
    expect(panel.hidden).toBe(true);
    dropdown.dispose();
  });
});
