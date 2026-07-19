/** Minimal typings for Clusterize.js (CDN global until Phase 7). */
interface ClusterizeOptions {
  rows: string[];
  scrollElem: HTMLElement;
  contentElem: HTMLElement;
  tag?: string;
  rows_in_block?: number;
  blocks_in_cluster?: number;
  callbacks?: {
    clusterChanged?: () => void;
  };
}

interface ClusterizeInstance {
  update(rows: string[]): void;
  refresh(force?: boolean): void;
  destroy(clean?: boolean): void;
}

interface ClusterizeConstructor {
  new (options: ClusterizeOptions): ClusterizeInstance;
}

declare const Clusterize: ClusterizeConstructor;

interface Window {
  Clusterize?: ClusterizeConstructor;
  QueryPlusSheetGrid?: import("../components/sheet-grid/types").QueryPlusSheetGridApi;
}
