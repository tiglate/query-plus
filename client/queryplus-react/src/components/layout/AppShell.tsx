import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import {
    ALargeSmall,
    Database,
    Folder,
    Gauge,
    Languages,
    LifeBuoy,
    ListChecks,
    LogOut,
    Minus,
    MoonStar,
    Plus,
    Settings,
    Terminal,
    User,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, NavLink, Outlet } from "react-router-dom";
import { submitLogout } from "@/api/client";
import { Button } from "@/components/ui/button";
import { applyTheme, changeFontSize, THEME_KEY, type Theme } from "@/lib/preferences";
import { setLocale, type SupportedLocale } from "@/i18n";
import { useUser } from "@/features/auth/useUser";
import {
    ADMIN_AREA_ROLES,
    CATEGORY_ROLES,
    EXECUTION_LOG_ROLES,
    JOB_ROLES,
    PROCEDURE_ROLES,
    hasAnyRole,
} from "@/features/auth/roles";

function navClass({ isActive }: { isActive: boolean }): string {
    return `inline-flex h-8 items-center gap-1.5 rounded px-2 text-body hover:bg-white/10 ${isActive ? "bg-white/10 text-cyan-400" : "text-white"}`;
}

export function AppShell() {
    const { t, i18n } = useTranslation();
    const user = useUser();
    const roles = user.data?.roles;
    const canSeeAdminArea = hasAnyRole(roles, ADMIN_AREA_ROLES);
    const canSeeCategories = hasAnyRole(roles, CATEGORY_ROLES);
    const canSeeProcedures = hasAnyRole(roles, PROCEDURE_ROLES);
    const canSeeExecutionLogs = hasAnyRole(roles, EXECUTION_LOG_ROLES);
    const canSeeJobs = hasAnyRole(roles, JOB_ROLES);
    const [theme, setTheme] = useState<Theme>(() => {
        const saved = localStorage.getItem(THEME_KEY);
        return saved === "light" || saved === "dark" || saved === "system" ? saved : "system";
    });

    useEffect(() => {
        if (theme !== "system") return;
        const media = window.matchMedia("(prefers-color-scheme: dark)");
        const listener = () => applyTheme("system");
        media.addEventListener("change", listener);
        return () => media.removeEventListener("change", listener);
    }, [theme]);

    const chooseTheme = (next: Theme) => {
        setTheme(next);
        applyTheme(next);
    };
    const logout = () => submitLogout();

    return (
        <div className="flex h-dvh flex-col overflow-hidden bg-slate-100 text-slate-900 dark:bg-navy-900 dark:text-slate-100">
            <header className="z-40 shrink-0 bg-gradient-to-b from-navy to-navy-600 text-white shadow">
                <div className="flex min-h-12 items-center justify-between gap-3 px-3 lg:px-4">
                    <div className="flex items-center gap-5">
                        <Link to="/" className="flex items-center gap-2 text-body font-semibold">
                            <span className="grid h-7 w-7 place-items-center rounded bg-cyan-500 text-small-label font-bold text-navy">
                                Q+
                            </span>
                            <span>QueryPlus</span>
                        </Link>
                        <nav className="hidden items-center gap-1 md:flex">
                            <NavLink to="/" end className={navClass}>
                                <Gauge className="h-4 w-4" />
                                {t("Nav_Home")}
                            </NavLink>
                            {canSeeAdminArea && (
                                <DropdownMenu.Root>
                                    <DropdownMenu.Trigger className="inline-flex h-8 items-center gap-1.5 rounded px-2 text-body hover:bg-white/10">
                                        <Settings className="h-4 w-4" />
                                        {t("Nav_Admin")}
                                    </DropdownMenu.Trigger>
                                    <DropdownMenu.Portal>
                                        <DropdownMenu.Content
                                            align="start"
                                            className="z-50 min-w-52 rounded-md border border-slate-200 bg-white p-1 text-slate-900 shadow-xl dark:border-navy-600 dark:bg-navy-800 dark:text-slate-100"
                                        >
                                            {canSeeCategories && (
                                                <DropdownMenu.Item asChild>
                                                    <Link
                                                        className="menu-item"
                                                        to="/admin/categories"
                                                    >
                                                        <Folder className="h-4 w-4" />
                                                        {t("Nav_Categories")}
                                                    </Link>
                                                </DropdownMenu.Item>
                                            )}
                                            {canSeeProcedures && (
                                                <DropdownMenu.Item asChild>
                                                    <Link
                                                        className="menu-item"
                                                        to="/admin/procedures"
                                                    >
                                                        <Database className="h-4 w-4" />
                                                        {t("Nav_Procedures")}
                                                    </Link>
                                                </DropdownMenu.Item>
                                            )}
                                            {canSeeExecutionLogs && (
                                                <DropdownMenu.Item asChild>
                                                    <Link
                                                        className="menu-item"
                                                        to="/admin/execution-logs"
                                                    >
                                                        <ListChecks className="h-4 w-4" />
                                                        {t("Nav_ExecutionLogs")}
                                                    </Link>
                                                </DropdownMenu.Item>
                                            )}
                                            {canSeeJobs && (
                                                <DropdownMenu.Item asChild>
                                                    <Link className="menu-item" to="/admin/jobs">
                                                        <Terminal className="h-4 w-4" />
                                                        {t("Nav_Jobs")}
                                                    </Link>
                                                </DropdownMenu.Item>
                                            )}
                                        </DropdownMenu.Content>
                                    </DropdownMenu.Portal>
                                </DropdownMenu.Root>
                            )}
                            <NavLink to="/support" className={navClass}>
                                <LifeBuoy className="h-4 w-4" />
                                {t("Nav_Support")}
                            </NavLink>
                        </nav>
                    </div>
                    <div className="flex items-center gap-1.5">
                        <Button
                            variant="ghost"
                            size="icon"
                            className="text-white hover:bg-white/10"
                            title={t("FontSize_Decrease")}
                            onClick={() => changeFontSize(-1)}
                        >
                            <Minus className="h-3 w-3" />
                        </Button>
                        <ALargeSmall className="hidden h-4 w-4 text-white/70 sm:block" />
                        <Button
                            variant="ghost"
                            size="icon"
                            className="text-white hover:bg-white/10"
                            title={t("FontSize_Increase")}
                            onClick={() => changeFontSize(1)}
                        >
                            <Plus className="h-3 w-3" />
                        </Button>
                        <MoonStar className="hidden h-4 w-4 sm:block" />
                        <select
                            value={theme}
                            onChange={(event) => chooseTheme(event.target.value as Theme)}
                            aria-label={t("Theme")}
                            className="header-select hidden sm:block"
                        >
                            <option value="system">{t("Theme_System")}</option>
                            <option value="light">{t("Theme_Light")}</option>
                            <option value="dark">{t("Theme_Dark")}</option>
                        </select>
                        <Languages className="hidden h-4 w-4 sm:block" />
                        <select
                            value={i18n.resolvedLanguage?.startsWith("en") ? "en" : "pt-BR"}
                            onChange={(event) =>
                                void setLocale(event.target.value as SupportedLocale)
                            }
                            aria-label={t("Language")}
                            className="header-select"
                        >
                            <option value="pt-BR">Português</option>
                            <option value="en">English</option>
                        </select>
                        {user.data?.isAuthenticated && (
                            <>
                                <span className="hidden items-center gap-1 text-small-label text-white/80 lg:inline-flex">
                                    <User className="h-3.5 w-3.5" />
                                    {user.data.username}
                                </span>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    className="text-white hover:bg-white/10"
                                    onClick={() => void logout()}
                                >
                                    <LogOut className="h-4 w-4" />
                                    <span className="hidden sm:inline">{t("Logout")}</span>
                                </Button>
                            </>
                        )}
                    </div>
                </div>
            </header>
            <main className="flex min-h-0 flex-1 flex-col overflow-auto">
                <Outlet />
            </main>
            <footer className="flex shrink-0 items-center justify-between border-t border-slate-200 bg-white px-4 py-2 text-caption text-slate-500 dark:border-navy-600 dark:bg-navy-800 dark:text-slate-400">
                <span>© {new Date().getFullYear()} QueryPlus</span>
                <span>{t("Footer_Tagline")}</span>
            </footer>
        </div>
    );
}
