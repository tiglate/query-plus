import { inject, injectable, singleton } from "tsyringe";
import { ConfirmSubmitService } from "@/components/confirm-submit/ConfirmSubmitService";
import { FontSizeService } from "@/components/font-size/FontSizeService";
import { LoadingBarService } from "@/components/loading-bar/LoadingBarService";
import { NavDropdownService } from "@/components/nav-dropdown/NavDropdownService";
import { ThemeService } from "@/components/theme/ThemeService";
import { ClientValidationService } from "@/core/ClientValidationService";
import { HtmxBridge } from "@/core/HtmxBridge";
import { PageController } from "@/core/PageController";
import { TOKENS } from "@/core/di/tokens";

/**
 * Layout-level behaviors: theme, nav, confirm forms, CSRF, client validation (no jQuery).
 */
@singleton()
@injectable()
export class SharedShellController extends PageController {
    constructor(
        @inject(TOKENS.Document) private readonly doc: Document,
        @inject(HtmxBridge) private readonly htmx: HtmxBridge,
        @inject(ThemeService) private readonly theme: ThemeService,
        @inject(FontSizeService) private readonly fontSize: FontSizeService,
        @inject(LoadingBarService) private readonly loadingBar: LoadingBarService,
        @inject(NavDropdownService) private readonly navDropdowns: NavDropdownService,
        @inject(ConfirmSubmitService)
        private readonly confirmSubmit: ConfirmSubmitService,
        @inject(ClientValidationService)
        private readonly clientValidation: ClientValidationService,
    ) {
        super();
    }

    mount(root: ParentNode = this.doc): void {
        this.htmx.wireCsrfFromMeta();
        this.theme.mount(root);
        this.fontSize.mount(root);
        this.loadingBar.mount(root);
        this.navDropdowns.mountAll(root);
        this.confirmSubmit.mount(root);
        this.clientValidation.mount(root);
    }

    unmount(): void {
        this.theme.dispose();
        this.fontSize.dispose();
        this.loadingBar.dispose();
        this.navDropdowns.dispose();
        this.confirmSubmit.dispose();
        this.clientValidation.dispose();
        this.htmx.dispose();
    }
}
