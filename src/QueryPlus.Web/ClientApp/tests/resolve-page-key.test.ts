import { describe, expect, it } from "vite-plus/test";
import { resolvePageKey } from "../src/core/pageKey";

describe("resolvePageKey", () => {
  it("ignores empty data-page on body and finds home on content", () => {
    document.body.innerHTML = "";
    document.body.setAttribute("data-page", "");
    document.body.innerHTML =
      '<div class="qp-home-page" data-page="home"></div>';
    // body attribute may be cleared by innerHTML assignment in jsdom — re-set.
    document.body.setAttribute("data-page", "");
    document.body.insertAdjacentHTML(
      "beforeend",
      '<div class="qp-home-page" data-page="home"></div>',
    );

    expect(resolvePageKey(document)).toBe("home");
  });

  it("prefers non-empty body data-page", () => {
    document.body.innerHTML = '<div data-page="home"></div>';
    document.body.setAttribute("data-page", "admin-categories");
    expect(resolvePageKey(document)).toBe("admin-categories");
  });

  it("returns empty string when no page key is set", () => {
    document.body.innerHTML = "<div></div>";
    document.body.removeAttribute("data-page");
    expect(resolvePageKey(document)).toBe("");
  });
});
