import { inject, injectable, singleton } from "tsyringe";
import { ParameterComboService } from "../../components/parameter-combo/ParameterComboService";
import { PageController } from "../../core/PageController";
import { TOKENS } from "../../core/di/tokens";
import { SyncMetadataService } from "./SyncMetadataService";

/**
 * Procedure Create / Edit / View: combo values visibility + sync metadata guards.
 */
@singleton()
@injectable()
export class AdminProcedureFormController extends PageController {
  constructor(
    @inject(TOKENS.Document) private readonly doc: Document,
    @inject(ParameterComboService)
    private readonly combo: ParameterComboService,
    @inject(SyncMetadataService) private readonly sync: SyncMetadataService,
  ) {
    super();
  }

  mount(root: ParentNode = this.doc): void {
    this.combo.mountAll(root);
    this.sync.mount(root);
  }

  unmount(): void {
    this.combo.dispose();
    this.sync.dispose();
  }
}
