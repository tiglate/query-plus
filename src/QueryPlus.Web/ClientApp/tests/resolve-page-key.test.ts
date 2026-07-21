import { describe, expect, it } from "vite-plus/test";
import { resolvePageKey } from "@/core/pageKey";

describe("resolvePageKey", () => {
    it("prefers meta[name=qp-page] over empty body data-page", () => {
        document.head.innerHTML = '<meta name="qp-page" content="home" />';
        document.body.innerHTML = '<div data-page="admin-categories"></div>';
        document.body.setAttribute("data-page", "");
        expect(resolvePageKey(document)).toBe("home");
    });

    it("ignores empty data-page on body and finds home on content", () => {
        document.head.innerHTML = "";
        document.body.innerHTML = "";
        document.body.setAttribute("data-page", "");
        document.body.insertAdjacentHTML(
            "beforeend",
            '<div class="qp-home-page" data-page="home"></div>',
        );
        expect(resolvePageKey(document)).toBe("home");
    });

    it("prefers non-empty body data-page when meta is empty", () => {
        document.head.innerHTML = '<meta name="qp-page" content="" />';
        document.body.innerHTML = '<div data-page="home"></div>';
        document.body.setAttribute("data-page", "admin-categories");
        expect(resolvePageKey(document)).toBe("admin-categories");
    });

    it("returns empty string when no page key is set", () => {
        document.head.innerHTML = "";
        document.body.innerHTML = "<div></div>";
        document.body.removeAttribute("data-page");
        expect(resolvePageKey(document)).toBe("");
    });
});
