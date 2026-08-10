import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Check,
    Eye,
    History,
    Pencil,
    Play,
    Plus,
    Power,
    Search,
    Send,
    Terminal,
    Trash2,
    X,
} from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import {
    approveJob,
    deleteJob,
    jobRunRequestQuery,
    jobsSearch,
    rejectJob,
    runJobNow,
    setJobEnabled,
    submitJobForApproval,
} from "@/api/queries";
import type { JobApprovalStatus, JobListItem } from "@/api/types";
import {
    JOB_APPROVAL_STATUS_BADGE,
    JOB_APPROVAL_STATUS_LABELS,
    JOB_TYPE_LABELS,
} from "@/api/types";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Select } from "@/components/ui/fields";
import { Input } from "@/components/ui/input";
import { Pager } from "@/components/ui/pager";
import { Field, PageHeader } from "@/components/ui/page";
import { useUser } from "@/features/auth/useUser";
import { JOB_APPROVE_ROLES, JOB_WRITE_ROLES, hasAnyRole } from "@/features/auth/roles";
import { useAdminSearch } from "../hooks/useAdminSearch";
import { RejectJobDialog } from "./RejectJobDialog";

interface JobFilters {
    [key: string]: string;
    name: string;
    approvalStatus: string;
    enabled: string;
}

const EMPTY_JOB_FILTERS: JobFilters = { name: "", approvalStatus: "", enabled: "" };

const DRAFT: JobApprovalStatus = 1;
const PENDING_APPROVAL: JobApprovalStatus = 2;
const APPROVED: JobApprovalStatus = 3;
const REJECTED: JobApprovalStatus = 4;

type ConfirmKind = "delete" | "approve" | "run-now";

export function JobsPage() {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const user = useUser();
    const roles = user.data?.roles;
    const username = user.data?.username;
    const canWrite = hasAnyRole(roles, JOB_WRITE_ROLES);
    const canApprove = hasAnyRole(roles, JOB_APPROVE_ROLES);
    const search = useAdminSearch("jobs", EMPTY_JOB_FILTERS, jobsSearch);
    const jobs = search.query;
    const [confirmAction, setConfirmAction] = useState<{
        kind: ConfirmKind;
        job: JobListItem;
    } | null>(null);
    const [rejecting, setRejecting] = useState<JobListItem | null>(null);
    const [pendingRun, setPendingRun] = useState<{ jobId: number; requestId: number } | null>(null);

    const invalidate = () => queryClient.invalidateQueries({ queryKey: ["jobs"] });

    // RequestRunNowAsync only queues a JobRunRequest - QueryPlus.SchedulerSync drains it into an
    // actual JobRun asynchronously, so jobRunId starts out null. Poll the request (same
    // refetchInterval shape as HomePage.tsx's export-status polling) until it's populated, then
    // hand the user a link into the run history instead of leaving Run Now looking like a no-op.
    const runRequest = useQuery({
        ...jobRunRequestQuery(pendingRun?.requestId ?? 0),
        enabled: pendingRun !== null,
        meta: { skipLoadingBar: true },
        // Also stop on error, not just once jobRunId is populated - otherwise a 404 (stale/
        // garbage-collected request id) or a transient failure leaves query.state.data undefined
        // forever and this polls at 1500ms indefinitely with the banner stuck on "Queued".
        refetchInterval: (query) =>
            query.state.status === "error" ? false : query.state.data?.jobRunId ? false : 1500,
    });

    const remove = useMutation({
        mutationFn: (id: number) => deleteJob(id),
        onSuccess: async () => {
            setConfirmAction(null);
            await invalidate();
        },
    });
    const submit = useMutation({
        mutationFn: (id: number) => submitJobForApproval(id),
        onSuccess: () => invalidate(),
    });
    const approve = useMutation({
        mutationFn: (id: number) => approveJob(id),
        onSuccess: async () => {
            setConfirmAction(null);
            await invalidate();
        },
    });
    const reject = useMutation({
        mutationFn: ({ id, reason }: { id: number; reason: string }) => rejectJob(id, { reason }),
        onSuccess: async () => {
            setRejecting(null);
            await invalidate();
        },
    });
    const toggleEnabled = useMutation({
        mutationFn: ({ id, enabled }: { id: number; enabled: boolean }) =>
            setJobEnabled(id, enabled),
        onSuccess: () => invalidate(),
    });
    const runNow = useMutation({
        mutationFn: (id: number) => runJobNow(id),
        onSuccess: async (request, jobId) => {
            setConfirmAction(null);
            setPendingRun({ jobId, requestId: request.id });
            await invalidate();
        },
    });

    const confirmTitle = () => {
        switch (confirmAction?.kind) {
            case "approve":
                return t("Jobs_ConfirmApprove");
            case "run-now":
                return t("Jobs_ConfirmRunNow");
            default:
                return t("ConfirmDelete");
        }
    };

    const runConfirmed = () => {
        if (!confirmAction) return;
        switch (confirmAction.kind) {
            case "delete":
                remove.mutate(confirmAction.job.id);
                break;
            case "approve":
                approve.mutate(confirmAction.job.id);
                break;
            case "run-now":
                runNow.mutate(confirmAction.job.id);
                break;
        }
    };

    return (
        <div className="flex min-h-0 flex-1 flex-col gap-3 p-4">
            <PageHeader
                title={t("Jobs_Title")}
                icon={<Terminal className="h-4 w-4 text-cyan-500" />}
                actions={
                    <>
                        <Button onClick={search.search}>
                            <Search className="h-4 w-4" />
                            {t("Search")}
                        </Button>
                        <Button variant="secondary" onClick={search.clear}>
                            {t("Clear")}
                        </Button>
                        {canWrite && (
                            <Button asChild variant="accent">
                                <Link to="/admin/jobs/new">
                                    <Plus className="h-4 w-4" />
                                    {t("Create")}
                                </Link>
                            </Button>
                        )}
                    </>
                }
            />
            {pendingRun && (
                <div className="flex items-center justify-between gap-3 rounded-md border border-slate-200 bg-white px-4 py-2 text-body dark:border-navy-600 dark:bg-navy-800">
                    {runRequest.data?.jobRunId ? (
                        <span>
                            {t("Jobs_RunNow")}: {t("Jobs_Run_Detail_Title")} #
                            {runRequest.data.jobRunId}
                        </span>
                    ) : (
                        <span>{t("Jobs_RunNow_Queued")}</span>
                    )}
                    <div className="flex items-center gap-2">
                        {runRequest.data?.jobRunId && (
                            <Button asChild size="sm" variant="secondary">
                                <Link to={`/admin/jobs/${pendingRun.jobId}/runs`}>
                                    <History className="h-3 w-3" />
                                    {t("Jobs_RunHistory")}
                                </Link>
                            </Button>
                        )}
                        <Button size="sm" variant="ghost" onClick={() => setPendingRun(null)}>
                            <X className="h-3 w-3" />
                        </Button>
                    </div>
                </div>
            )}
            <Card>
                <CardHeader>
                    <h2 className="text-card-title font-semibold">{t("Filter")}</h2>
                </CardHeader>
                <CardBody className="grid gap-3 md:grid-cols-6">
                    <Field label={t("Jobs_Name")}>
                        <Input
                            value={search.draft.name}
                            onChange={(e) => search.updateDraft("name", e.target.value)}
                        />
                    </Field>
                    <Field label={t("Jobs_ApprovalStatus")}>
                        <Select
                            value={search.draft.approvalStatus}
                            onChange={(e) => search.updateDraft("approvalStatus", e.target.value)}
                        >
                            <option value="">—</option>
                            <option value="1">{t("JobApprovalStatus_Draft")}</option>
                            <option value="2">{t("JobApprovalStatus_PendingApproval")}</option>
                            <option value="3">{t("JobApprovalStatus_Approved")}</option>
                            <option value="4">{t("JobApprovalStatus_Rejected")}</option>
                        </Select>
                    </Field>
                    <Field label={t("Enabled")}>
                        <Select
                            value={search.draft.enabled}
                            onChange={(e) => search.updateDraft("enabled", e.target.value)}
                        >
                            <option value="">—</option>
                            <option value="true">{t("Yes")}</option>
                            <option value="false">{t("No")}</option>
                        </Select>
                    </Field>
                    <Field label={t("Pagination_PageSize")}>
                        <Select
                            value={search.pageSize}
                            onChange={(e) => search.changePageSize(Number(e.target.value))}
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
                                <th>{t("Jobs_Name")}</th>
                                <th>{t("Jobs_JobType")}</th>
                                <th>{t("Jobs_ApprovalStatus")}</th>
                                <th>{t("Enabled")}</th>
                                <th>{t("Jobs_CronExpression")}</th>
                                <th>{t("Jobs_RunAsUser")}</th>
                                <th>{t("CreatedBy")}</th>
                                <th>{t("Jobs_ApprovedBy")}</th>
                                <th className="text-center!">{t("Actions")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {jobs.data?.items.map((job) => {
                                const isOwnJob = !!username && job.createdBy === username;
                                return (
                                    <tr key={job.id}>
                                        <td className="text-right">{job.id}</td>
                                        <td>{job.name}</td>
                                        <td>{t(JOB_TYPE_LABELS[job.jobType])}</td>
                                        <td>
                                            <Badge
                                                variant={
                                                    JOB_APPROVAL_STATUS_BADGE[job.approvalStatus]
                                                }
                                            >
                                                {t(JOB_APPROVAL_STATUS_LABELS[job.approvalStatus])}
                                            </Badge>
                                        </td>
                                        <td>
                                            <Badge variant={job.enabled ? "success" : "neutral"}>
                                                {t(job.enabled ? "Enabled" : "Disabled")}
                                            </Badge>
                                        </td>
                                        <td className="font-mono">{job.cronExpression}</td>
                                        <td>{job.runAsUser}</td>
                                        <td>{job.createdBy}</td>
                                        <td>{job.approvedBy ?? "—"}</td>
                                        <td>
                                            <div className="flex flex-wrap justify-center gap-1">
                                                <Button asChild size="sm" variant="ghost">
                                                    <Link to={`/admin/jobs/${job.id}?mode=view`}>
                                                        <Eye className="h-3 w-3" />
                                                        {t("View")}
                                                    </Link>
                                                </Button>
                                                {canWrite && (
                                                    <Button asChild size="sm" variant="ghost">
                                                        <Link to={`/admin/jobs/${job.id}`}>
                                                            <Pencil className="h-3 w-3" />
                                                            {t("Edit")}
                                                        </Link>
                                                    </Button>
                                                )}
                                                <Button asChild size="sm" variant="ghost">
                                                    <Link to={`/admin/jobs/${job.id}/runs`}>
                                                        <History className="h-3 w-3" />
                                                        {t("Jobs_RunHistory")}
                                                    </Link>
                                                </Button>
                                                {canWrite &&
                                                    (job.approvalStatus === DRAFT ||
                                                        job.approvalStatus === REJECTED) && (
                                                        <Button
                                                            size="sm"
                                                            variant="ghost"
                                                            onClick={() => submit.mutate(job.id)}
                                                        >
                                                            <Send className="h-3 w-3" />
                                                            {t("Jobs_Submit")}
                                                        </Button>
                                                    )}
                                                {canApprove &&
                                                    job.approvalStatus === PENDING_APPROVAL &&
                                                    !isOwnJob && (
                                                        <Button
                                                            size="sm"
                                                            variant="ghost"
                                                            onClick={() =>
                                                                setConfirmAction({
                                                                    kind: "approve",
                                                                    job,
                                                                })
                                                            }
                                                        >
                                                            <Check className="h-3 w-3" />
                                                            {t("Jobs_Approve")}
                                                        </Button>
                                                    )}
                                                {canApprove &&
                                                    job.approvalStatus === PENDING_APPROVAL && (
                                                        <Button
                                                            size="sm"
                                                            variant="ghost"
                                                            className="text-danger"
                                                            onClick={() => setRejecting(job)}
                                                        >
                                                            <X className="h-3 w-3" />
                                                            {t("Jobs_Reject")}
                                                        </Button>
                                                    )}
                                                {canWrite && job.approvalStatus === APPROVED && (
                                                    <Button
                                                        size="sm"
                                                        variant="ghost"
                                                        onClick={() =>
                                                            toggleEnabled.mutate({
                                                                id: job.id,
                                                                enabled: !job.enabled,
                                                            })
                                                        }
                                                    >
                                                        <Power className="h-3 w-3" />
                                                        {t(job.enabled ? "Disabled" : "Enabled")}
                                                    </Button>
                                                )}
                                                {canWrite &&
                                                    job.approvalStatus === APPROVED &&
                                                    job.enabled && (
                                                        <Button
                                                            size="sm"
                                                            variant="ghost"
                                                            onClick={() =>
                                                                setConfirmAction({
                                                                    kind: "run-now",
                                                                    job,
                                                                })
                                                            }
                                                        >
                                                            <Play className="h-3 w-3" />
                                                            {t("Jobs_RunNow")}
                                                        </Button>
                                                    )}
                                                {canWrite && job.approvalStatus === DRAFT && (
                                                    <Button
                                                        size="sm"
                                                        variant="ghost"
                                                        className="text-danger"
                                                        onClick={() =>
                                                            setConfirmAction({
                                                                kind: "delete",
                                                                job,
                                                            })
                                                        }
                                                    >
                                                        <Trash2 className="h-3 w-3" />
                                                        {t("Delete")}
                                                    </Button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                );
                            })}
                            {!jobs.data?.items.length && (
                                <tr>
                                    <td colSpan={10} className="p-8 text-center text-muted">
                                        {t("NoRecords")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
                {search.total > 0 && (
                    <Pager
                        page={search.page}
                        pageSize={search.pageSize}
                        total={search.total}
                        onPage={search.setPage}
                    />
                )}
            </Card>
            <ConfirmDialog
                open={!!confirmAction}
                title={confirmTitle()}
                description={confirmAction?.job.name}
                onOpenChange={(open) => !open && setConfirmAction(null)}
                onConfirm={runConfirmed}
            />
            <RejectJobDialog
                open={!!rejecting}
                jobName={rejecting?.name}
                onOpenChange={(open) => !open && setRejecting(null)}
                onConfirm={(reason) => rejecting && reject.mutate({ id: rejecting.id, reason })}
            />
        </div>
    );
}
