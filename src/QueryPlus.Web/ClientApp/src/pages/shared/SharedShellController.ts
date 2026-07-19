import { inject, injectable, singleton } from "tsyringe";
import { ConfirmSubmitService } from "../../components/confirm-submit/ConfirmSubmitService";
import { NavDropdownService } from "../../components/nav-dropdown/NavDropdownService";
import { HtmxBridge } from "../../core/HtmxBridge";
import { PageController } from "../../core/PageController";
import { TOKENS } from "../../core/di/tokens";

/**
 * Layout-level behaviors present on every page: nav dropdowns, confirm forms, CSRF.
 */
@singleton()
@injectable()
export class SharedShellController extends PageController {
  constructor(
    @inject(TOKENS.Document) private readonly doc: Document,
    @inject(HtmxBridge) private readonly htmx: HtmxBridge,
    @inject(NavDropdownService) private readonly navDropdowns: NavDropdownService,
    @inject(ConfirmSubmitService)
    private readonly confirmSubmit: ConfirmSubmitService,
  ) {
    super();
  }

  mount(root: ParentNode = this.doc): void {
    this.htmx.wireCsrfFromMeta();
    this.navDropdowns.mountAll(root);
    this.confirmSubmit.mount(root);
  }

  unmount(): void {
    this.navDropdowns.dispose();
    this.confirmSubmit.dispose();
    this.htmx.dispose();
  }
}
