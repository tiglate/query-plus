import { inject, injectable, singleton } from "tsyringe";
import { SheetGridService } from "@/components/sheet-grid/SheetGridService";
import { PageController } from "@/core/PageController";
import { TOKENS } from "@/core/di/tokens";

const ADMIN_SHEET_SELECTOR = ".js-sheet-root.qp-sheet-grid--admin";

/**
 * Admin list screens (Categories / Procedures index): mount shared sheet grids.
 */
@singleton()
@injectable()
export class AdminListPageController extends PageController {
    constructor(
        @inject(TOKENS.Document) private readonly doc: Document,
        @inject(SheetGridService) private readonly sheetGrid: SheetGridService,
    ) {
        super();
    }

    mount(root: ParentNode = this.doc): void {
        root.querySelectorAll(ADMIN_SHEET_SELECTOR).forEach((el) => {
            this.sheetGrid.mount(el);
        });
    }

    unmount(): void {
        this.doc.querySelectorAll(ADMIN_SHEET_SELECTOR).forEach((el) => {
            this.sheetGrid.destroy(el);
        });
    }
}
