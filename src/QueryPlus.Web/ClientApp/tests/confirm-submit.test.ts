import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "../src/core/di/container";
import { ConfirmSubmitService } from "../src/components/confirm-submit/ConfirmSubmitService";

describe("ConfirmSubmitService", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  it("prevents submit when confirm returns false", () => {
    const form = document.createElement("form");
    form.dataset.confirm = "Delete this item?";
    document.body.appendChild(form);

    const confirmFn = vi.fn(() => false);
    const c = createTestContainer({ confirmFn });
    const service = c.resolve(ConfirmSubmitService);
    service.mount(document);

    const event = new Event("submit", { bubbles: true, cancelable: true });
    const prevented = !form.dispatchEvent(event);

    expect(confirmFn).toHaveBeenCalledWith("Delete this item?");
    expect(prevented || event.defaultPrevented).toBe(true);

    service.dispose();
  });

  it("allows submit when confirm returns true", () => {
    const form = document.createElement("form");
    form.dataset.confirm = "Delete this item?";
    document.body.appendChild(form);

    const confirmFn = vi.fn(() => true);
    const c = createTestContainer({ confirmFn });
    const service = c.resolve(ConfirmSubmitService);
    service.mount(document);

    const event = new Event("submit", { bubbles: true, cancelable: true });
    form.dispatchEvent(event);

    expect(confirmFn).toHaveBeenCalledWith("Delete this item?");
    expect(event.defaultPrevented).toBe(false);

    service.dispose();
  });

  it("ignores forms without data-confirm", () => {
    const form = document.createElement("form");
    document.body.appendChild(form);

    const confirmFn = vi.fn(() => false);
    const c = createTestContainer({ confirmFn });
    const service = c.resolve(ConfirmSubmitService);
    service.mount(document);

    const event = new Event("submit", { bubbles: true, cancelable: true });
    form.dispatchEvent(event);

    expect(confirmFn).not.toHaveBeenCalled();
    expect(event.defaultPrevented).toBe(false);

    service.dispose();
  });
});
