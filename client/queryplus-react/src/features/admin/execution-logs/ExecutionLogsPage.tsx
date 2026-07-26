import { useQuery } from "@tanstack/react-query";
import { ListChecks, Search } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "@/api/client";
import { executionLogsSearch } from "@/api/queries";
import type { ProcedureLookup } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { Select } from "@/components/ui/fields";
import { Input } from "@/components/ui/input";
import { Pager } from "@/components/ui/pager";
import { Field, PageHeader } from "@/components/ui/page";

interface LogFilters {
    username: string;
    procedureId: string;
    success: string;
    startFrom: string;
    startTo: string;
}

function duration(start: string, end: string | null): string {
    if (!end) return "—";
    const milliseconds = new Date(end).getTime() - new Date(start).getTime();
    if (milliseconds < 0) return "—";
    if (milliseconds < 1000) return `${milliseconds}ms`;
    if (milliseconds < 60_000) return `${(milliseconds / 1000).toFixed(1)}s`;
    return `${Math.floor(milliseconds / 60_000)}m ${String(Math.floor(milliseconds / 1000) % 60).padStart(2, "0")}s`;
}

export function ExecutionLogsPage() {
    const { t } = useTranslation();
    const empty: LogFilters = {
        username: "",
        procedureId: "",
        success: "",
        startFrom: "",
        startTo: "",
    };
    const [draft, setDraft] = useState(empty);
    const [filter, setFilter] = useState(empty);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const options = useQuery({
        queryKey: ["execution-logs", "procedures"],
        queryFn: () => apiFetch<ProcedureLookup[]>("/api/execution-logs/procedures"),
    });
    const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize) });
    (Object.keys(filter) as Array<keyof LogFilters>).forEach((key) => {
        const value = filter[key];
        if (value) params.set(key, value);
    });
    const logs = useQuery({
        queryKey: ["execution-logs", params.toString()],
        queryFn: () => executionLogsSearch(params),
    });
    const update = (key: keyof LogFilters, value: string) =>
        setDraft((current) => ({ ...current, [key]: value }));
    const total = logs.data?.totalCount ?? 0;
    return (
        <div className="flex min-h-0 flex-1 flex-col gap-3 p-4">
            <PageHeader
                title={t("ExecutionLog_Title")}
                icon={<ListChecks className="h-4 w-4 text-cyan-500" />}
                actions={
                    <>
                        <Button
                            onClick={() => {
                                setFilter(draft);
                                setPage(1);
                            }}
                        >
                            <Search className="h-4 w-4" />
                            {t("Search")}
                        </Button>
                        <Button
                            variant="secondary"
                            onClick={() => {
                                setDraft(empty);
                                setFilter(empty);
                                setPage(1);
                            }}
                        >
                            {t("Clear")}
                        </Button>
                    </>
                }
            />
            <Card>
                <CardHeader>
                    <h2 className="text-card-title font-semibold">{t("Filter")}</h2>
                </CardHeader>
                <CardBody className="grid gap-3 md:grid-cols-6">
                    <Field label={t("ExecutionLog_Username")}>
                        <Input
                            value={draft.username}
                            onChange={(e) => update("username", e.target.value)}
                        />
                    </Field>
                    <Field label={t("ExecutionLog_Procedure")}>
                        <Select
                            value={draft.procedureId}
                            onChange={(e) => update("procedureId", e.target.value)}
                        >
                            <option value="">—</option>
                            {options.data?.map((procedure) => (
                                <option key={procedure.id} value={procedure.id}>
                                    {procedure.caption}
                                </option>
                            ))}
                        </Select>
                    </Field>
                    <Field label={t("ExecutionLog_Status")}>
                        <Select
                            value={draft.success}
                            onChange={(e) => update("success", e.target.value)}
                        >
                            <option value="">—</option>
                            <option value="true">{t("ExecutionLog_StatusSuccess")}</option>
                            <option value="false">{t("ExecutionLog_StatusFailed")}</option>
                        </Select>
                    </Field>
                    <Field label={t("Pagination_PageSize")}>
                        <Select
                            value={pageSize}
                            onChange={(e) => {
                                setPageSize(Number(e.target.value));
                                setPage(1);
                            }}
                        >
                            {[10, 20, 50, 100].map((size) => (
                                <option key={size}>{size}</option>
                            ))}
                        </Select>
                    </Field>
                    <Field label={t("ExecutionLog_StartFrom")}>
                        <Input
                            type="date"
                            value={draft.startFrom}
                            onChange={(e) => update("startFrom", e.target.value)}
                        />
                    </Field>
                    <Field label={t("ExecutionLog_StartTo")}>
                        <Input
                            type="date"
                            value={draft.startTo}
                            onChange={(e) => update("startTo", e.target.value)}
                        />
                    </Field>
                </CardBody>
            </Card>
            <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
                <CardHeader>
                    <h2 className="text-card-title font-semibold">{t("Home_Results")}</h2>
                </CardHeader>
                <div className="min-h-0 flex-1 overflow-auto">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>{t("Id")}</th>
                                <th>{t("ExecutionLog_Procedure")}</th>
                                <th>{t("ExecutionLog_Username")}</th>
                                <th>{t("ExecutionLog_IpAddress")}</th>
                                <th>{t("ExecutionLog_StartedAt")}</th>
                                <th>{t("ExecutionLog_Duration")}</th>
                                <th>{t("ExecutionLog_Status")}</th>
                                <th>{t("ExecutionLog_RowCount")}</th>
                                <th>{t("ExecutionLog_Error")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {logs.data?.items.map((log) => (
                                <tr key={log.id}>
                                    <td className="text-right">{log.id}</td>
                                    <td>{log.procedureCaption}</td>
                                    <td>{log.username}</td>
                                    <td>{log.ipAddress ?? "—"}</td>
                                    <td>{new Date(log.executionStart).toLocaleString()}</td>
                                    <td className="text-right">
                                        {duration(log.executionStart, log.executionEnd)}
                                    </td>
                                    <td>
                                        <span className={log.success ? "badge-ok" : "badge-danger"}>
                                            {t(
                                                log.success
                                                    ? "ExecutionLog_StatusSuccess"
                                                    : "ExecutionLog_StatusFailed",
                                            )}
                                        </span>
                                    </td>
                                    <td className="text-right">{log.rowCount ?? "—"}</td>
                                    <td
                                        className="max-w-xs truncate text-red-700"
                                        title={log.errorMessage ?? undefined}
                                    >
                                        {log.errorMessage ?? "—"}
                                    </td>
                                </tr>
                            ))}
                            {!logs.data?.items.length && (
                                <tr>
                                    <td colSpan={9} className="p-8 text-center text-slate-500">
                                        {t("NoRecords")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
                {total > 0 && (
                    <Pager page={page} pageSize={pageSize} total={total} onPage={setPage} />
                )}
            </Card>
        </div>
    );
}
