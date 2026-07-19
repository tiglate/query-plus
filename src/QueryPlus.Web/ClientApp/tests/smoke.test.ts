import { describe, expect, it } from "vite-plus/test";
import {
  markClientAppLoaded,
  QUERYPLUS_CLIENT_VERSION,
} from "../src/entries/app";

describe("ClientApp smoke", () => {
  it("exports a client version marker", () => {
    expect(QUERYPLUS_CLIENT_VERSION).toMatch(/phase4/);
  });

  it("runs under jsdom", () => {
    const el = document.createElement("div");
    el.dataset.page = "home";
    document.body.appendChild(el);
    expect(document.querySelector("[data-page='home']")).not.toBeNull();
  });

  it("marks the document root when loaded", () => {
    markClientAppLoaded(document);
    expect(document.documentElement.getAttribute("data-qp-client")).toBe(
      QUERYPLUS_CLIENT_VERSION,
    );
  });
});
