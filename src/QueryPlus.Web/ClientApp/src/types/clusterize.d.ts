/** Minimal typings for Clusterize.js (npm `clusterize.js`). */
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

declare module "clusterize.js" {
  const Clusterize: ClusterizeConstructor;
  export default Clusterize;
}

interface Window {
  Clusterize?: ClusterizeConstructor;
  htmx?: unknown;
  QueryPlusSheetGrid?: import("@/components/sheet-grid/types").QueryPlusSheetGridApi;
}
