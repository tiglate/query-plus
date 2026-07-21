import { afterEach, beforeEach, describe, expect, it } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { HtmxBridge } from "@/core/HtmxBridge";

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

    it("dispatches beforeRequest/afterRequest to registered handlers", () => {
        const c = createTestContainer();
        const bridge = c.resolve(HtmxBridge);

        const starts: unknown[] = [];
        const ends: unknown[] = [];
        bridge.onBeforeRequest((event) => starts.push((event as CustomEvent).detail));
        bridge.onAfterRequest((event) => ends.push((event as CustomEvent).detail));

        const elt = document.createElement("button");
        document.body.appendChild(elt);

        elt.dispatchEvent(
            new CustomEvent("htmx:beforeRequest", { bubbles: true, detail: { elt } }),
        );
        elt.dispatchEvent(new CustomEvent("htmx:afterRequest", { bubbles: true, detail: { elt } }));

        expect(starts).toHaveLength(1);
        expect(ends).toHaveLength(1);
        bridge.dispose();
    });
});
