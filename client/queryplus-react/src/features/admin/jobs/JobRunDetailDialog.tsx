import * as Dialog from "@radix-ui/react-dialog";
import { useQuery } from "@tanstack/react-query";
import { X } from "lucide-react";
import { useTranslation } from "react-i18next";
import { jobRunLogQuery, jobRunQuery } from "@/api/queries";
import {
    JOB_RUN_STATUS_BADGE,
    JOB_RUN_STATUS_LABELS,
    JOB_TRIGGER_SOURCE_LABELS,
    isTerminalJobRunStatus,
} from "@/api/types";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

interface JobRunDetailDialogProps {
    open: boolean;
    runId: number | null;
    onOpenChange: (open: boolean) => void;
}

export function JobRunDetailDialog({
    open,
    runId,
    onOpenChange,
}: Readonly<JobRunDetailDialogProps>) {
    const { t } = useTranslation();
    const id = runId ?? 0;
    const run = useQuery({
        ...jobRunQuery(id),
        enabled: open && id > 0,
        meta: { skipLoadingBar: true },
        refetchInterval: (query) => {
            // Stop on error too, not just on a successful terminal status - otherwise a 404/500/
            // network failure leaves query.state.data undefined forever and this keeps returning
            // 1500 indefinitely instead of ever settling.
            if (query.state.status === "error") {
                return false;
            }
            const status = query.state.data?.status;
            return status && isTerminalJobRunStatus(status) ? false : 1500;
        },
    });
    // Logs poll in lockstep with the run's own status query (not their own data) so the <pre>
    // blocks keep refreshing while a run is still active instead of freezing at whatever was
    // fetched on dialog open - typically empty, since the runner has barely started writing.
    const runStatus = run.data?.status;
    const logsShouldPoll = !runStatus || !isTerminalJobRunStatus(runStatus);
    const stdout = useQuery({
        ...jobRunLogQuery(id, "Stdout"),
        enabled: open && id > 0,
        meta: { skipLoadingBar: true },
        refetchInterval: (query) =>
            query.state.status === "error" ? false : logsShouldPoll && 1500,
    });
    const stderr = useQuery({
        ...jobRunLogQuery(id, "Stderr"),
        enabled: open && id > 0,
        meta: { skipLoadingBar: true },
        refetchInterval: (query) =>
            query.state.status === "error" ? false : logsShouldPoll && 1500,
    });

    return (
        <Dialog.Root open={open} onOpenChange={onOpenChange}>
            <Dialog.Portal>
                <Dialog.Overlay className="fixed inset-0 z-50 bg-black/45" />
                <Dialog.Content className="fixed left-1/2 top-1/2 z-50 flex max-h-[85vh] w-[min(56rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 flex-col rounded-lg bg-white p-5 shadow-xl dark:bg-navy-800">
                    <div className="flex items-center justify-between">
                        <Dialog.Title className="text-card-title font-semibold">
                            {t("Jobs_Run_Detail_Title")} #{id}
                        </Dialog.Title>
                        <Dialog.Close asChild>
                            <Button type="button" variant="ghost" size="icon">
                                <X className="h-4 w-4" />
                            </Button>
                        </Dialog.Close>
                    </div>
                    {run.data && (
                        <div className="mt-3 grid grid-cols-2 gap-3 text-dense md:grid-cols-4">
                            <span>
                                {t("Jobs_Run_Status")}
                                <strong className="block">
                                    <Badge variant={JOB_RUN_STATUS_BADGE[run.data.status]}>
                                        {t(JOB_RUN_STATUS_LABELS[run.data.status])}
                                    </Badge>
                                </strong>
                            </span>
                            <span>
                                {t("Jobs_Run_TriggeredBy")}
                                <strong className="block">
                                    {t(JOB_TRIGGER_SOURCE_LABELS[run.data.triggeredBy])}
                                </strong>
                            </span>
                            <span>
                                {t("Jobs_Run_StartedAt")}
                                <strong className="block">
                                    {run.data.startedAt
                                        ? new Date(run.data.startedAt).toLocaleString()
                                        : "—"}
                                </strong>
                            </span>
                            <span>
                                {t("Jobs_Run_FinishedAt")}
                                <strong className="block">
                                    {run.data.finishedAt
                                        ? new Date(run.data.finishedAt).toLocaleString()
                                        : "—"}
                                </strong>
                            </span>
                            <span>
                                {t("Jobs_Run_ExitCode")}
                                <strong className="block">{run.data.exitCode ?? "—"}</strong>
                            </span>
                            <span>
                                {t("Jobs_Run_HostMachine")}
                                <strong className="block">{run.data.hostMachine ?? "—"}</strong>
                            </span>
                            <span>
                                {t("Jobs_Run_RunnerPid")}
                                <strong className="block">{run.data.runnerPid ?? "—"}</strong>
                            </span>
                            <span>
                                {t("Jobs_Run_ChildPid")}
                                <strong className="block">{run.data.childPid ?? "—"}</strong>
                            </span>
                        </div>
                    )}
                    <div className="mt-4 min-h-0 flex-1 space-y-3 overflow-y-auto">
                        <div>
                            <h3 className="text-small-label font-semibold text-slate-700 dark:text-slate-200">
                                {t("Jobs_Run_Stdout")}
                            </h3>
                            <pre className="mt-1 max-h-56 overflow-auto rounded bg-slate-900 p-3 text-caption text-slate-100">
                                {stdout.data || "—"}
                            </pre>
                        </div>
                        <div>
                            <h3 className="text-small-label font-semibold text-slate-700 dark:text-slate-200">
                                {t("Jobs_Run_Stderr")}
                            </h3>
                            <pre className="mt-1 max-h-56 overflow-auto rounded bg-slate-900 p-3 text-caption text-danger-100">
                                {stderr.data || "—"}
                            </pre>
                        </div>
                    </div>
                </Dialog.Content>
            </Dialog.Portal>
        </Dialog.Root>
    );
}
