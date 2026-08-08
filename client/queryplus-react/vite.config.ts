import path from "node:path";
import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig, loadEnv } from "vite";

const root = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, root, "");
    const proxyTarget = env.VITE_API_PROXY || "http://localhost:5132";

    return {
        plugins: [react(), tailwindcss()],
        resolve: { alias: { "@": path.resolve(root, "src") } },
        server: {
            port: 5173,
            strictPort: true,
            proxy: {
                "/api": { target: proxyTarget, changeOrigin: true },
                "/login": { target: proxyTarget, changeOrigin: true },
            },
        },
        build: {
            outDir: "../../src/QueryPlus.Api/wwwroot",
            emptyOutDir: true,
            sourcemap: "hidden",
            rollupOptions: {
                output: {
                    entryFileNames: "assets/queryplus.js",
                    assetFileNames: "assets/[name][extname]",
                    // Enforces the single-bundle invariant by config, not just by the current
                    // absence of dynamic import()/React.lazy() - a future dynamic import would
                    // otherwise silently produce a second chunk and reintroduce the
                    // ResultsMaximize double-mount bug this setup exists to avoid.
                    inlineDynamicImports: true,
                },
            },
        },
        test: {
            globals: true,
            environment: "jsdom",
            setupFiles: ["./src/test/setup.ts"],
            css: true,
        },
    };
});
