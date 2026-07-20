import path from "node:path";
import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite-plus";

const rootDir = path.dirname(fileURLToPath(import.meta.url));
const clientApp = path.resolve(rootDir, "ClientApp");

/**
 * Multi-entry ClientApp build for Razor (no SPA).
 * Outputs to wwwroot/dist — gitignored; produced on dev/build/publish.
 */
export default defineConfig({
  plugins: [tailwindcss()],
  root: rootDir,
  publicDir: false,
  // ASP.NET serves built assets under /dist/* (wwwroot/dist). Without this,
  // font/CSS url() paths resolve to /fonts/... and 404.
  base: "/dist/",
  resolve: {
    alias: {
      "@": path.resolve(clientApp, "src"),
    },
  },
  server: {
    // Assets are built into wwwroot; HMR is optional during early phases.
    // Prefer `vp build --watch` / `vp dev` with outDir for ASP.NET static files.
    origin: "http://localhost:5173",
  },
  build: {
    outDir: path.resolve(rootDir, "wwwroot/dist"),
    emptyOutDir: true,
    manifest: true,
    sourcemap: true,
    rollupOptions: {
      input: {
        app: path.resolve(clientApp, "src/entries/app.ts"),
      },
      output: {
        // Single entry bundle: avoids async vendor chunk doing
        // `import … from "./app.js"`. MapStaticAssets fingerprints the HTML
        // entry URL (app.xxxxx.js) while that relative import still targets
        // plain app.js — a second module instance double-mounted handlers
        // (Maximize toggled on then off in one click).
        inlineDynamicImports: true,
        entryFileNames: "js/[name].js",
        chunkFileNames: "js/[name]-[hash].js",
        assetFileNames: (assetInfo) => {
          const name = assetInfo.names?.[0] ?? assetInfo.name ?? "asset";
          if (name.endsWith(".css") || name.includes(".css")) {
            return "css/site.css";
          }
          // Font Awesome / Inter webfonts
          if (/\.(woff2?|ttf|eot|svg)$/i.test(name)) {
            return "fonts/[name]-[hash][extname]";
          }
          return "assets/[name]-[hash][extname]";
        },
      },
    },
  },
  test: {
    environment: "jsdom",
    include: ["ClientApp/tests/**/*.{test,spec}.ts"],
    globals: false,
  },
});
