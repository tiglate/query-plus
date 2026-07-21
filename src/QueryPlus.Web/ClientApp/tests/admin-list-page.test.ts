import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { SheetGridService } from "@/components/sheet-grid/SheetGridService";
import { AdminListPageController } from "@/pages/admin/AdminListPageController";

describe("AdminListPageController", () => {
    beforeEach(() => {
        document.body.innerHTML = `
      <div class="js-sheet-root qp-sheet-grid--admin" id="grid-a"></div>
      <div class="js-sheet-root" id="grid-other"></div>
    `;
    });

    afterEach(() => {
        document.body.innerHTML = "";
        vi.restoreAllMocks();
    });

    it("mounts only admin sheet roots", () => {
        const c = createTestContainer();
        const sheets = c.resolve(SheetGridService);
        const mountSpy = vi.spyOn(sheets, "mount").mockReturnValue(null);

        const page = c.resolve(AdminListPageController);
        page.mount(document);

        expect(mountSpy).toHaveBeenCalledTimes(1);
        expect(mountSpy.mock.calls[0][0]).toBe(document.getElementById("grid-a"));

        page.dispose();
    });
});
