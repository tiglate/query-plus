import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Eye, History } from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { jobQuery, jobRunsSearch } from "@/api/queries";
import {
    JOB_RUN_STATUS_BADGE,
    JOB_RUN_STATUS_LABELS,
    JOB_TRIGGER_SOURCE_LABELS,
    isTerminalJobRunStatus,
} from "@/api/types";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { Select } from "@/components/ui/fields";
import { Pager } from "@/components/ui/pager";
import { Field, PageHeader } from "@/components/ui/page";
import { JobRunDetailDialog } from "./JobRunDetailDialog";

export function JobRunHistoryPage() {
    const { t } = useTranslation();
    const { id } = useParams();
    const jobDefinitionId = Number(id);
    const job = useQuery(jobQuery(jobDefinitionId));
    const [status, setStatus] = useState("");
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [selectedRunId, setSelectedRunId] = useState<number | null>(null);

    const params = new URLSearchParams({
        jobDefinitionId: String(jobDefinitionId),
        pageNumber: String(page),
        pageSize: String(pageSize),
    });
    if (status) params.set("status", status);
    const paramsKey = params.toString();

    const runs = useQuery({
        queryKey: ["jobs", "runs", "search", paramsKey],
        queryFn: () => jobRunsSearch(params),
        // Mirrors HomePage.tsx's export-status polling shape: keep refreshing while any row on
        // the current page is still in flight, stop once everything visible is terminal.
        refetchInterval: (query) => {
            const items = query.state.data?.items ?? [];
            return items.some((item) => !isTerminalJobRunStatus(item.status)) ? 1500 : false;
        },
    });

    useEffect(() => {
        if (runs.data && runs.data.page !== page) setPage(runs.data.page);
        // eslint-disable-next-line react-hooks/exhaustive-deps -- only react to the server's page
    }, [runs.data?.page]);

    return (
        <div className="flex min-h-0 flex-1 flex-col gap-3 p-4">
            <PageHeader
                title={`${t("Jobs_RunHistory")}${job.data ? `: ${job.data.name}` : ""}`}
                icon={<History className="h-4 w-4 text-cyan-500" />}
                actions={
                    <Button asChild variant="secondary">
                        <Link to="/admin/jobs">
                            <ArrowLeft className="h-4 w-4" />
                            {t("Back")}
                        </Link>
                    </Button>
                }
            />
            <Card>
                <CardHeader>
                    <h2 className="text-card-title font-semibold">{t("Filter")}</h2>
                </CardHeader>
                <CardBody className="grid gap-3 md:grid-cols-6">
                    <Field label={t("Jobs_Run_Status")}>
                        <Select
                            value={status}
                            onChange={(e) => {
                                setStatus(e.target.value);
                                setPage(1);
                            }}
                        >
                            <option value="">—</option>
                            <option value="1">{t("JobRunStatus_Queued")}</option>
                            <option value="2">{t("JobRunStatus_Starting")}</option>
                            <option value="3">{t("JobRunStatus_Running")}</option>
                            <option value="4">{t("JobRunStatus_Succeeded")}</option>
                            <option value="5">{t("JobRunStatus_Failed")}</option>
                            <option value="6">{t("JobRunStatus_Lost")}</option>
                            <option value="7">{t("JobRunStatus_MissedTrigger")}</option>
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
                                <th>{t("Jobs_Run_Status")}</th>
                                <th>{t("Jobs_Run_TriggeredBy")}</th>
                                <th>{t("Jobs_Run_StartedAt")}</th>
                                <th>{t("Jobs_Run_FinishedAt")}</th>
                                <th>{t("Jobs_Run_ExitCode")}</th>
                                <th>{t("Jobs_Run_HostMachine")}</th>
                                <th className="text-center!">{t("Actions")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {runs.data?.items.map((run) => (
                                <tr key={run.id}>
                                    <td className="text-right">{run.id}</td>
                                    <td>
                                        <Badge variant={JOB_RUN_STATUS_BADGE[run.status]}>
                                            {t(JOB_RUN_STATUS_LABELS[run.status])}
                                        </Badge>
                                    </td>
                                    <td>{t(JOB_TRIGGER_SOURCE_LABELS[run.triggeredBy])}</td>
                                    <td>
                                        {run.startedAt
                                            ? new Date(run.startedAt).toLocaleString()
                                            : "—"}
                                    </td>
                                    <td>
                                        {run.finishedAt
                                            ? new Date(run.finishedAt).toLocaleString()
                                            : "—"}
                                    </td>
                                    <td className="text-right">{run.exitCode ?? "—"}</td>
                                    <td>{run.hostMachine ?? "—"}</td>
                                    <td>
                                        <div className="flex justify-center gap-1">
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                onClick={() => setSelectedRunId(run.id)}
                                            >
                                                <Eye className="h-3 w-3" />
                                                {t("View")}
                                            </Button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                            {!runs.data?.items.length && (
                                <tr>
                                    <td colSpan={8} className="p-8 text-center text-muted">
                                        {t("NoRecords")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
                {!!runs.data?.totalCount && (
                    <Pager
                        page={page}
                        pageSize={pageSize}
                        total={runs.data.totalCount}
                        onPage={setPage}
                    />
                )}
            </Card>
            <JobRunDetailDialog
                open={selectedRunId !== null}
                runId={selectedRunId}
                onOpenChange={(open) => !open && setSelectedRunId(null)}
            />
        </div>
    );
}
