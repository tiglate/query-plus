import { CircleAlert, Home, LifeBuoy } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Link, isRouteErrorResponse, useRouteError } from "react-router-dom";
import { Button } from "@/components/ui/button";

export function NotFoundPage() {
    const { t } = useTranslation();
    return (
        <div className="grid min-h-full place-items-center p-6 text-center">
            <div className="max-w-xl">
                <div className="mx-auto mb-4 grid h-16 w-16 place-items-center rounded-full bg-cyan-100 text-cyan-700">
                    <CircleAlert className="h-8 w-8" />
                </div>
                <h1 className="text-display-lg font-bold">{t("Error_NotFound_Title")}</h1>
                <p className="mt-2 font-medium text-slate-600 dark:text-slate-300">
                    {t("Error_NotFound_Lead")}
                </p>
                <p className="mt-2 text-body text-muted">{t("Error_NotFound_Body")}</p>
                <Button asChild className="mt-5">
                    <Link to="/">
                        <Home className="h-4 w-4" />
                        {t("Error_GoHome")}
                    </Link>
                </Button>
            </div>
        </div>
    );
}

export function RouteErrorPage() {
    const { t } = useTranslation();
    const error = useRouteError();
    const status = isRouteErrorResponse(error) ? error.status : 500;
    const message = error instanceof Error ? error.message : t("Error_Server_Body");
    return (
        <div className="grid min-h-dvh place-items-center bg-slate-100 p-6 text-center dark:bg-navy-900">
            <div className="max-w-xl rounded-lg bg-white p-8 shadow dark:bg-navy-800">
                <CircleAlert className="mx-auto h-12 w-12 text-danger" />
                <h1 className="mt-4 text-display-sm font-bold">
                    {t("Error_Server_Title").replace("{0}", String(status))}
                </h1>
                <p className="mt-2 text-body text-slate-600 dark:text-slate-300">{message}</p>
                <div className="mt-5 flex justify-center gap-2">
                    <Button asChild>
                        <Link to="/">
                            <Home className="h-4 w-4" />
                            {t("Error_GoHome")}
                        </Link>
                    </Button>
                    <Button asChild variant="secondary">
                        <Link to="/support">
                            <LifeBuoy className="h-4 w-4" />
                            {t("Error_ContactSupport")}
                        </Link>
                    </Button>
                </div>
            </div>
        </div>
    );
}
