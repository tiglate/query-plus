/**
 * DEPRECATED (Phase 2): Sheet grid lives in ClientApp.
 * See ClientApp/src/components/sheet-grid/ — window.QueryPlusSheetGrid is
 * installed by dist/js/app.js (SheetGridService.installGlobalBridge).
 * This file is kept only as a short-term reference and will be removed in Phase 6.
 */
(function () {
  "use strict";
  if (typeof console !== "undefined" && console.warn) {
    console.warn(
      "[QueryPlus] wwwroot/js/sheet-grid.js is deprecated; use ClientApp SheetGridService via dist/js/app.js."
    );
  }
})();
