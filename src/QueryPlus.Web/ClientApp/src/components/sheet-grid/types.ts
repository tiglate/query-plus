export interface SheetColumn {
    caption: string;
    align: string;
    html: boolean;
    sortable: boolean;
    width: number;
    fixedWidth: boolean;
}

export interface SheetPayloadColumn {
    caption?: string;
    align?: string;
    html?: boolean;
    sortable?: boolean;
    width?: number;
}

export interface SheetPayload {
    columns?: SheetPayloadColumn[];
    cells?: string[][];
}

export interface SheetGridState {
    root: HTMLElement;
    columns: SheetColumn[];
    cells: string[][];
    sortCol: number | null;
    sortAsc: boolean;
    suppressClick: boolean;
    clusterize: ClusterizeInstance | null;
    onScroll: (() => void) | null;
}

export interface QueryPlusSheetGridApi {
    mount(root: Element | null | undefined): SheetGridState | null;
    mountAll(selector: string): void;
    destroy(root: Element): void;
    refresh(root: Element): void;
    getState(root: Element): SheetGridState | null;
}
