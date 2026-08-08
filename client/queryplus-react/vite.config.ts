import path from "node:path";
import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig, loadEnv } from "vite";
import { VitePWA } from "vite-plugin-pwa";

const root = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, root, "");
    const proxyTarget = env.VITE_API_PROXY || "http://localhost:5132";

    return {
        plugins: [
            react(),
            tailwindcss(),
            VitePWA({
                registerType: "autoUpdate",
                injectRegister: "auto",
                manifest: {
                    id: "/",
                    name: "QueryPlus",
                    short_name: "QueryPlus",
                    description: "Governed queries with security",
                    lang: "pt-BR",
                    start_url: "/",
                    scope: "/",
                    display: "standalone",
                    theme_color: "#1c334d",
                    background_color: "#1c334d",
                    icons: [
                        {
                            src: "/pwa/icon-any-192.png",
                            sizes: "192x192",
                            type: "image/png",
                            purpose: "any",
                        },
                        {
                            src: "/pwa/icon-any-512.png",
                            sizes: "512x512",
                            type: "image/png",
                            purpose: "any",
                        },
                        {
                            src: "/pwa/icon-maskable-512.png",
                            sizes: "512x512",
                            type: "image/png",
                            purpose: "maskable",
                        },
                    ],
                },
                workbox: {
                    // Only the built SPA shell is precached; /api and /login must always hit the
                    // network — they carry authenticated, per-user data and (for /login) a
                    // server-side OIDC redirect that a cached shell would otherwise short-circuit.
                    globPatterns: ["**/*.{js,css,html,svg,png,ico,webmanifest}"],
                    navigateFallbackDenylist: [/^\/api\//, /^\/login\b/],
                    // vite-plugin-pwa defaults to skipping revision hashes for anything under
                    // assets/, assuming Vite's normal content-hashed filenames make that redundant.
                    // This project deliberately serves a single, unhashed assets/queryplus.js (see
                    // the rollupOptions comment above) so that assumption doesn't hold here - without
                    // this override, redeploys would keep the same URL/null revision and the service
                    // worker would never notice the bundle changed. Force a real content hash instead.
                    dontCacheBustURLsMatching: /^$/,
                },
            }),
        ],
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
