import { afterEach, beforeEach, describe, expect, it } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "../src/core/di/container";
import { HtmxBridge } from "../src/core/HtmxBridge";

describe("HtmxBridge", () => {
  beforeEach(() => {
    document.head.innerHTML = "";
    document.body.innerHTML = "";
  });

  afterEach(() => {
    document.head.innerHTML = "";
    document.body.innerHTML = "";
  });

  it("injects CSRF token from meta on configRequest", () => {
    const meta = document.createElement("meta");
    meta.name = "csrf-token";
    meta.content = "test-token-123";
    document.head.appendChild(meta);

    const c = createTestContainer();
    const bridge = c.resolve(HtmxBridge);
    bridge.wireCsrfFromMeta();

    const headers: Record<string, string> = {};
    const event = new CustomEvent("htmx:configRequest", {
      detail: { headers },
    });
    document.body.dispatchEvent(event);

    expect(headers["RequestVerificationToken"]).toBe("test-token-123");
    bridge.dispose();
  });
});
