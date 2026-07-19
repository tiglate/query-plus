import { afterEach, beforeEach, describe, expect, it } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "../src/core/di/container";
import { canSyncMetadata } from "../src/pages/admin/syncMetadata";
import { SyncMetadataService } from "../src/pages/admin/SyncMetadataService";

describe("canSyncMetadata", () => {
  it("requires both database and procedure", () => {
    expect(canSyncMetadata("", "")).toBe(false);
    expect(canSyncMetadata("db", "")).toBe(false);
    expect(canSyncMetadata("", "sp")).toBe(false);
    expect(canSyncMetadata(" db ", " sp ")).toBe(true);
  });
});

describe("SyncMetadataService", () => {
  beforeEach(() => {
    document.body.innerHTML = `
      <form id="procedure-form">
        <input class="js-sync-metadata-source" data-sync-field="database" value="" />
        <input class="js-sync-metadata-source" data-sync-field="procedure" value="" />
        <button type="submit" id="btn-sync-metadata">Sync</button>
        <p id="sync-metadata-hint">Fill fields</p>
      </form>
    `;
  });

  afterEach(() => {
    document.body.innerHTML = "";
  });

  it("disables button until both fields are filled", () => {
    const c = createTestContainer();
    const service = c.resolve(SyncMetadataService);
    service.mount(document);

    const btn = document.getElementById(
      "btn-sync-metadata",
    ) as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
    expect(
      document.getElementById("sync-metadata-hint")!.classList.contains(
        "hidden",
      ),
    ).toBe(false);

    const db = document.querySelector(
      '[data-sync-field="database"]',
    ) as HTMLInputElement;
    const proc = document.querySelector(
      '[data-sync-field="procedure"]',
    ) as HTMLInputElement;
    db.value = "QueryPlus";
    db.dispatchEvent(new Event("input"));
    expect(btn.disabled).toBe(true);

    proc.value = "Sp_Demo";
    proc.dispatchEvent(new Event("input"));
    expect(btn.disabled).toBe(false);
    expect(
      document.getElementById("sync-metadata-hint")!.classList.contains(
        "hidden",
      ),
    ).toBe(true);

    service.dispose();
  });

  it("prevents submit when fields empty", () => {
    const c = createTestContainer();
    const service = c.resolve(SyncMetadataService);
    service.mount(document);

    const form = document.getElementById("procedure-form") as HTMLFormElement;
    const btn = document.getElementById(
      "btn-sync-metadata",
    ) as HTMLButtonElement;

    const event = new Event("submit", { bubbles: true, cancelable: true });
    Object.defineProperty(event, "submitter", { value: btn });
    form.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    service.dispose();
  });
});
