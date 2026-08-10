import { createBrowserRouter } from "react-router-dom";
import { authQuery } from "@/api/queries";
import { AppShell } from "@/components/layout/AppShell";
import { CategoriesPage } from "@/features/admin/categories/CategoriesPage";
import { ExecutionLogsPage } from "@/features/admin/execution-logs/ExecutionLogsPage";
import { JobEditorPage } from "@/features/admin/jobs/JobEditorPage";
import { JobRunHistoryPage } from "@/features/admin/jobs/JobRunHistoryPage";
import { JobsPage } from "@/features/admin/jobs/JobsPage";
import { ProcedureEditorPage } from "@/features/admin/procedures/ProcedureEditorPage";
import { ProceduresPage } from "@/features/admin/procedures/ProceduresPage";
import {
    CATEGORY_ROLES,
    EXECUTION_LOG_ROLES,
    JOB_ROLES,
    JOB_WRITE_ROLES,
    PROCEDURE_ROLES,
    hasAnyRole,
} from "@/features/auth/roles";
import { HomePage } from "@/features/home/HomePage";
import { SupportPage } from "@/features/support/SupportPage";
import { NotFoundPage, RouteErrorPage } from "./errors";
import { queryClient } from "./queryClient";

export async function authLoader({ request }: { request: Request }) {
    const user = await queryClient.ensureQueryData(authQuery);
    if (!user.isAuthenticated) {
        const url = new URL(request.url);
        const returnUrl = `${url.pathname}${url.search}${url.hash}`;
        window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
        // window.location.assign() schedules a full-page navigation but doesn't halt this
        // loader - without this, React Router would render <AppShell> and its children for at
        // least one frame while the navigation is still in flight. A Response/redirect throw
        // would swap that flash for the errorElement's flash instead (this route has one); a
        // never-resolving promise is what actually suspends rendering until the real
        // navigation lands.
        await new Promise<never>(() => {});
    }
    return user;
}

/**
 * Server-side (API) enforcement is the real gate; this only avoids rendering an admin page
 * that would just 403 on every request, so an unauthorized user who navigates or bookmarks
 * an admin URL directly gets a clear "forbidden" page instead of a broken/empty one.
 */
export function requireAnyRole(required: readonly string[]) {
    return async () => {
        const user = await queryClient.ensureQueryData(authQuery);
        if (!hasAnyRole(user.roles, required)) {
            throw new Response("Forbidden", { status: 403 });
        }
        return null;
    };
}

export const router = createBrowserRouter([
    {
        path: "/",
        loader: authLoader,
        element: <AppShell />,
        errorElement: <RouteErrorPage />,
        children: [
            { index: true, element: <HomePage />, errorElement: <RouteErrorPage /> },
            {
                path: "admin/categories",
                element: <CategoriesPage />,
                loader: requireAnyRole(CATEGORY_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/procedures",
                element: <ProceduresPage />,
                loader: requireAnyRole(PROCEDURE_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/procedures/new",
                element: <ProcedureEditorPage />,
                loader: requireAnyRole(PROCEDURE_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/procedures/:id",
                element: <ProcedureEditorPage />,
                loader: requireAnyRole(PROCEDURE_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/execution-logs",
                element: <ExecutionLogsPage />,
                loader: requireAnyRole(EXECUTION_LOG_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/jobs",
                element: <JobsPage />,
                loader: requireAnyRole(JOB_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/jobs/new",
                element: <JobEditorPage />,
                loader: requireAnyRole(JOB_WRITE_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/jobs/:id",
                element: <JobEditorPage />,
                loader: requireAnyRole(JOB_ROLES),
                errorElement: <RouteErrorPage />,
            },
            {
                path: "admin/jobs/:id/runs",
                element: <JobRunHistoryPage />,
                loader: requireAnyRole(JOB_ROLES),
                errorElement: <RouteErrorPage />,
            },
            { path: "support", element: <SupportPage />, errorElement: <RouteErrorPage /> },
            { path: "*", element: <NotFoundPage />, errorElement: <RouteErrorPage /> },
        ],
    },
]);
