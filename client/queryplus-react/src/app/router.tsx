import { createBrowserRouter } from "react-router-dom";
import { authQuery } from "@/api/queries";
import { AppShell } from "@/components/layout/AppShell";
import { CategoriesPage } from "@/features/admin/categories/CategoriesPage";
import { ExecutionLogsPage } from "@/features/admin/execution-logs/ExecutionLogsPage";
import { ProcedureEditorPage } from "@/features/admin/procedures/ProcedureEditorPage";
import { ProceduresPage } from "@/features/admin/procedures/ProceduresPage";
import { LoginRedirect } from "@/features/auth/LoginRedirect";
import { HomePage } from "@/features/home/HomePage";
import { SupportPage } from "@/features/support/SupportPage";
import { NotFoundPage, RouteErrorPage } from "./errors";
import { queryClient } from "./queryClient";

async function authLoader({ request }: { request: Request }) {
    const user = await queryClient.ensureQueryData(authQuery);
    if (!user.isAuthenticated) {
        const url = new URL(request.url);
        const returnUrl = `${url.pathname}${url.search}${url.hash}`;
        window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
    }
    return user;
}

export const router = createBrowserRouter([
    { path: "/login", element: <LoginRedirect />, errorElement: <RouteErrorPage /> },
    {
        path: "/",
        loader: authLoader,
        element: <AppShell />,
        errorElement: <RouteErrorPage />,
        children: [
            { index: true, element: <HomePage /> },
            { path: "admin/categories", element: <CategoriesPage /> },
            { path: "admin/procedures", element: <ProceduresPage /> },
            { path: "admin/procedures/new", element: <ProcedureEditorPage /> },
            { path: "admin/procedures/:id", element: <ProcedureEditorPage /> },
            { path: "admin/execution-logs", element: <ExecutionLogsPage /> },
            { path: "support", element: <SupportPage /> },
            { path: "*", element: <NotFoundPage /> },
        ],
    },
]);
