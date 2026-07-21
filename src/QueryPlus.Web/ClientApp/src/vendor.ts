/**
 * Local npm vendors (no CDN). Loaded before app bootstrap (not in unit tests).
 * HTMX + Clusterize are exposed on window for attributes and SheetGrid.
 */
import htmx from "htmx.org";
import Clusterize from "clusterize.js";

const win = window as Window & {
    htmx?: typeof htmx;
    Clusterize?: typeof Clusterize;
};

win.htmx = htmx;
win.Clusterize = Clusterize as unknown as typeof Clusterize;

export { htmx, Clusterize };
