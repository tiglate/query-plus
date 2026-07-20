import { inject, injectable, singleton } from "tsyringe";
import { ConfirmSubmitService } from "../../components/confirm-submit/ConfirmSubmitService";
import { NavDropdownService } from "../../components/nav-dropdown/NavDropdownService";
import { ThemeService } from "../../components/theme/ThemeService";
import { ClientValidationService } from "../../core/ClientValidationService";
import { HtmxBridge } from "../../core/HtmxBridge";
import { PageController } from "../../core/PageController";
import { TOKENS } from "../../core/di/tokens";

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
    this.navDropdowns.mountAll(root);
    this.confirmSubmit.mount(root);
    this.clientValidation.mount(root);
  }

  unmount(): void {
    this.theme.dispose();
    this.navDropdowns.dispose();
    this.confirmSubmit.dispose();
    this.clientValidation.dispose();
    this.htmx.dispose();
  }
}
