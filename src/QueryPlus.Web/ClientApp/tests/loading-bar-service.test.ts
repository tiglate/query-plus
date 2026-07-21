import { afterEach, beforeEach, describe, expect, it } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { LoadingBarService } from "@/components/loading-bar/LoadingBarService";

function dispatch(name: string, elt: Element): void {
    elt.dispatchEvent(new CustomEvent(name, { bubbles: true, detail: { elt } }));
}

describe("LoadingBarService", () => {
    let bar: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = `<div id="qp-loading-bar"></div>`;
        bar = document.getElementById("qp-loading-bar")!;
    });

    afterEach(() => {
        document.body.innerHTML = "";
    });

    it("activates on request start and deactivates on request end", () => {
        const c = createTestContainer();
        const service = c.resolve(LoadingBarService);
        service.mount();

        const trigger = document.createElement("button");
        document.body.appendChild(trigger);

        dispatch("htmx:beforeRequest", trigger);
        expect(bar.classList.contains("is-active")).toBe(true);

        dispatch("htmx:afterRequest", trigger);
        expect(bar.classList.contains("is-active")).toBe(false);

        service.dispose();
    });

    it("stays active while any of several overlapping requests is in flight", () => {
        const c = createTestContainer();
        const service = c.resolve(LoadingBarService);
        service.mount();

        const a = document.createElement("button");
        const b = document.createElement("button");
        document.body.append(a, b);

        dispatch("htmx:beforeRequest", a);
        dispatch("htmx:beforeRequest", b);
        expect(bar.classList.contains("is-active")).toBe(true);

        dispatch("htmx:afterRequest", a);
        expect(bar.classList.contains("is-active")).toBe(true);

        dispatch("htmx:afterRequest", b);
        expect(bar.classList.contains("is-active")).toBe(false);

        service.dispose();
    });

    it("ignores elements opted out via data-loading-indicator=skip", () => {
        const c = createTestContainer();
        const service = c.resolve(LoadingBarService);
        service.mount();

        const polling = document.createElement("div");
        polling.setAttribute("data-loading-indicator", "skip");
        document.body.appendChild(polling);

        dispatch("htmx:beforeRequest", polling);
        expect(bar.classList.contains("is-active")).toBe(false);

        dispatch("htmx:afterRequest", polling);
        expect(bar.classList.contains("is-active")).toBe(false);

        service.dispose();
    });

    it("clears state on dispose", () => {
        const c = createTestContainer();
        const service = c.resolve(LoadingBarService);
        service.mount();

        const trigger = document.createElement("button");
        document.body.appendChild(trigger);

        dispatch("htmx:beforeRequest", trigger);
        expect(bar.classList.contains("is-active")).toBe(true);

        service.dispose();
        expect(bar.classList.contains("is-active")).toBe(false);

        // Further events must not be handled after dispose.
        dispatch("htmx:beforeRequest", trigger);
        expect(bar.classList.contains("is-active")).toBe(false);
    });
});
