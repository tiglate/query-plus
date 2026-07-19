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

  it("does not prompt again on a follow-up submit after accept", () => {
    const form = document.createElement("form");
    form.dataset.confirm = "Delete this item?";
    document.body.appendChild(form);

    const confirmFn = vi.fn(() => true);
    const c = createTestContainer({ confirmFn });
    const service = c.resolve(ConfirmSubmitService);
    service.mount(document);

    form.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
    form.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));

    expect(confirmFn).toHaveBeenCalledTimes(1);

    service.dispose();
  });

  it("stops further handlers when confirm is cancelled", () => {
    const form = document.createElement("form");
    form.dataset.confirm = "Delete?";
    document.body.appendChild(form);

    const confirmFn = vi.fn(() => false);
    const later = vi.fn();
    document.body.addEventListener("submit", later, true);

    const c = createTestContainer({ confirmFn });
    const service = c.resolve(ConfirmSubmitService);
    service.mount(document);

    // Register our service after `later` would normally order — capture + stopImmediate
    // still prevents later capture listeners registered after mount if we mount first.
    // Mount first, then add another capture listener that should not run on cancel.
    document.body.removeEventListener("submit", later, true);
    const afterConfirm = vi.fn();
    document.body.addEventListener("submit", afterConfirm, true);

    const event = new Event("submit", { bubbles: true, cancelable: true });
    form.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    // Handlers registered after ours in capture phase are skipped via stopImmediatePropagation.
    expect(afterConfirm).not.toHaveBeenCalled();

    document.body.removeEventListener("submit", afterConfirm, true);
    service.dispose();
  });
});
