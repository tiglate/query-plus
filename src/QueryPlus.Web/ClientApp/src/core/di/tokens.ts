/**
 * DI tokens. Prefer explicit @inject(TOKENS.*) so Vite/esbuild does not need
 * emitDecoratorMetadata (tsyringe still works without TypeScript metadata emit).
 */
export const TOKENS = {
    Document: Symbol.for("qp.Document"),
    Window: Symbol.for("qp.Window"),
    ConfirmFn: Symbol.for("qp.ConfirmFn"),
} as const;

export type ConfirmFn = (message: string) => boolean;
