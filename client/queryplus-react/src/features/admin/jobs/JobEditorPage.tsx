import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Save, Terminal, Upload, Wand2 } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { z } from "zod";
import { ApiError } from "@/api/client";
import { createJob, jobQuery, jobRunAsUsersQuery, updateJob, uploadJobScript } from "@/api/queries";
import type { JobDetail, JobInput, JobType } from "@/api/types";
import { JOB_APPROVAL_STATUS_BADGE, JOB_APPROVAL_STATUS_LABELS } from "@/api/types";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Select, Textarea } from "@/components/ui/fields";
import { Input } from "@/components/ui/input";
import { Field, PageHeader, Section } from "@/components/ui/page";
import { JOB_WRITE_ROLES, hasAnyRole } from "@/features/auth/roles";
import { useUser } from "@/features/auth/useUser";
import { CronExpressionBuilderDialog } from "./CronExpressionBuilderDialog";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const jobSchema = z
    .object({
        name: z.string().trim().min(1).max(200),
        description: z.string().nullable(),
        jobType: z.coerce.number().int().min(1).max(2),
        cronExpression: z.string().trim().min(1),
        runAsUser: z.string().trim().min(1),
        memoryLimitMb: z.coerce.number().int().positive(),
        maxDurationMinutes: z.coerce.number().int().positive(),
        notifyEmails: z.string().nullable(),
    })
    .superRefine((value, context) => {
        const raw = value.notifyEmails?.trim();
        if (!raw) return;
        const hasInvalid = raw
            .split(",")
            .map((email) => email.trim())
            .filter((email) => email !== "")
            .some((email) => !EMAIL_RE.test(email));
        if (hasInvalid) {
            context.addIssue({
                code: "custom",
                path: ["notifyEmails"],
                message: "Comma-separated list of valid email addresses required",
            });
        }
    });

export type JobFormValues = z.input<typeof jobSchema>;

export function jobFormToApi(values: JobFormValues, id?: number): JobInput {
    const parsed = jobSchema.parse(values);
    return {
        id,
        name: parsed.name.trim(),
        description: parsed.description?.trim() || null,
        jobType: parsed.jobType as JobType,
        cronExpression: parsed.cronExpression.trim(),
        runAsUser: parsed.runAsUser.trim(),
        memoryLimitMb: parsed.memoryLimitMb,
        maxDurationMinutes: parsed.maxDurationMinutes,
        notifyEmails: parsed.notifyEmails?.trim() || null,
    };
}

const defaults: JobFormValues = {
    name: "",
    description: "",
    jobType: 1,
    cronExpression: "",
    runAsUser: "",
    memoryLimitMb: 512,
    maxDurationMinutes: 60,
    notifyEmails: "",
};

const EDITABLE_FIELD_KEYS = new Set(Object.keys(defaults));

function detailToForm(detail: JobDetail): JobFormValues {
    return {
        name: detail.name,
        description: detail.description ?? "",
        jobType: detail.jobType,
        cronExpression: detail.cronExpression,
        runAsUser: detail.runAsUser,
        memoryLimitMb: detail.memoryLimitMb,
        maxDurationMinutes: detail.maxDurationMinutes,
        notifyEmails: detail.notifyEmails ?? "",
    };
}

export function JobEditorPage() {
    const { t } = useTranslation();
    const route = useParams();
    const [search] = useSearchParams();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const user = useUser();
    const id = route.id && route.id !== "new" ? Number(route.id) : undefined;
    // Server is the real enforcement (403 on write without ROLE_JOB_WRITE) - forcing read-only
    // here just avoids handing a write-role-less user an editable form whose Save always fails.
    const canWrite = hasAnyRole(user.data?.roles, JOB_WRITE_ROLES);
    const readOnly = search.get("mode") === "view" || !canWrite;
    const detail = useQuery(jobQuery(id ?? 0));
    const runAsUsers = useQuery({ ...jobRunAsUsersQuery, enabled: !readOnly });
    const form = useForm<JobFormValues>({
        resolver: zodResolver(jobSchema),
        defaultValues: defaults,
    });
    const [cronBuilderOpen, setCronBuilderOpen] = useState(false);
    const [scriptFile, setScriptFile] = useState<File | null>(null);
    const [scriptInputKey, setScriptInputKey] = useState(0);

    useEffect(() => {
        form.reset(detail.data ? detailToForm(detail.data) : defaults);
    }, [detail.data, form, id]);

    const uploadScript = useMutation({
        mutationFn: ({ file, jobId }: { file: File; jobId: number }) =>
            uploadJobScript(jobId, file),
        onSuccess: async (_data, variables) => {
            setScriptFile(null);
            setScriptInputKey((key) => key + 1);
            await queryClient.invalidateQueries({ queryKey: ["jobs", variables.jobId] });
        },
    });

    const save = useMutation({
        mutationFn: (values: JobFormValues) =>
            id ? updateJob(id, jobFormToApi(values, id)) : createJob(jobFormToApi(values)),
        onSuccess: async (saved) => {
            // For a brand-new job, upload whatever script the analyst already picked before
            // saving - the endpoint needs a real job id, which only exists from this point on. A
            // failed upload here still leaves the job itself created; the error surfaces below the
            // script row (uploadScript.error) and the file can be re-selected and uploaded from the
            // now-existing job's edit page.
            if (!id && scriptFile) {
                await uploadScript
                    .mutateAsync({ file: scriptFile, jobId: saved.id })
                    .catch(() => {});
            }
            await queryClient.invalidateQueries({ queryKey: ["jobs"] });
            navigate(`/admin/jobs/${saved.id}`);
        },
        onError: (error) => {
            if (!(error instanceof ApiError)) return;
            const details = error.details;
            if (!details || typeof details !== "object") return;
            for (const [field, messages] of Object.entries(details.errors ?? {})) {
                const key = field.charAt(0).toLowerCase() + field.slice(1);
                if (EDITABLE_FIELD_KEYS.has(key) && messages[0]) {
                    form.setError(key as keyof JobFormValues, { message: messages[0] });
                }
            }
        },
    });

    let pageTitle;
    if (readOnly) {
        pageTitle = t("Jobs_View");
    } else if (id) {
        pageTitle = t("Jobs_Edit");
    } else {
        pageTitle = t("Jobs_New");
    }

    let scriptStatusMessage: string;
    if (!id) {
        scriptStatusMessage = scriptFile
            ? t("Jobs_Script_WillUploadOnSave", { filename: scriptFile.name })
            : t("Jobs_Script_SelectPrompt");
    } else if (detail.data?.scriptPath) {
        scriptStatusMessage = t(
            detail.data.jobType === 2
                ? "Jobs_Script_Uploaded_Python"
                : "Jobs_Script_Uploaded_Shell",
        );
    } else {
        scriptStatusMessage = t("Jobs_Script_NotUploaded");
    }

    const knownRunAsUsers = runAsUsers.data ?? [];
    const detailRunAsUser = detail.data?.runAsUser;
    const runAsUserOptions =
        detailRunAsUser && !knownRunAsUsers.includes(detailRunAsUser)
            ? [...knownRunAsUsers, detailRunAsUser]
            : knownRunAsUsers;
    let runAsUserControl;
    if (readOnly) {
        runAsUserControl = <Input readOnly {...form.register("runAsUser")} />;
    } else if (runAsUsers.isLoading) {
        runAsUserControl = (
            <Select disabled>
                <option>{t("Jobs_RunAsUser_Loading")}</option>
            </Select>
        );
    } else if (runAsUserOptions.length > 0) {
        runAsUserControl = (
            <Select {...form.register("runAsUser")}>
                <option value="">{t("Jobs_RunAsUser_Select")}</option>
                {runAsUserOptions.map((username) => (
                    <option key={username} value={username}>
                        {username}
                    </option>
                ))}
            </Select>
        );
    } else {
        runAsUserControl = <Input {...form.register("runAsUser")} />;
    }

    return (
        <form
            className="space-y-4 p-4"
            onSubmit={form.handleSubmit((values) => save.mutate(values))}
        >
            <PageHeader
                title={pageTitle}
                icon={<Terminal className="h-4 w-4 text-cyan-500" />}
                actions={
                    <>
                        <Button asChild type="button" variant="secondary">
                            <Link to="/admin/jobs">
                                <ArrowLeft className="h-4 w-4" />
                                {t("Back")}
                            </Link>
                        </Button>
                        {!readOnly && (
                            <Button type="submit" disabled={save.isPending}>
                                <Save className="h-4 w-4" />
                                {t("Save")}
                            </Button>
                        )}
                    </>
                }
            />
            {Object.keys(form.formState.errors).length > 0 && (
                <div className="rounded border border-danger-line bg-danger-subtle p-3 text-body text-danger">
                    {t("Validation_FixErrors")}
                </div>
            )}
            <Section title={t("Jobs_Title")}>
                <div className="grid gap-4 lg:grid-cols-3">
                    <Field
                        label={t("Jobs_Name")}
                        required
                        error={form.formState.errors.name?.message}
                    >
                        <Input readOnly={readOnly} {...form.register("name")} />
                    </Field>
                    <Field
                        label={t("Jobs_JobType")}
                        required
                        error={form.formState.errors.jobType?.message}
                    >
                        <Select
                            disabled={readOnly}
                            {...form.register("jobType", { valueAsNumber: true })}
                        >
                            <option value={1}>{t("JobType_Shell")}</option>
                            <option value={2}>{t("JobType_Python")}</option>
                        </Select>
                    </Field>
                    <Field label={t("Jobs_Script")}>
                        <div className="flex items-center gap-2">
                            <Input
                                key={scriptInputKey}
                                type="file"
                                accept=".sh,.py"
                                className="w-auto min-w-0 flex-1"
                                disabled={readOnly}
                                onChange={(event) => setScriptFile(event.target.files?.[0] ?? null)}
                            />
                            {/* For a new (unsaved) job there's no id to upload against yet - the
                                selected file is queued in scriptFile and uploaded automatically
                                right after Save creates the job, so no separate button is needed
                                here. For an existing job, this uploads/replaces the script
                                immediately without requiring a full form Save. */}
                            {id && (
                                <Button
                                    type="button"
                                    variant="secondary"
                                    className="shrink-0"
                                    disabled={readOnly || !scriptFile || uploadScript.isPending}
                                    onClick={() => {
                                        if (scriptFile)
                                            uploadScript.mutate({ file: scriptFile, jobId: id });
                                    }}
                                >
                                    <Upload className="h-4 w-4" />
                                    {t("Jobs_Script_Upload")}
                                </Button>
                            )}
                        </div>
                        <span className="mt-1 block text-small-label text-muted">
                            {scriptStatusMessage}
                        </span>
                        {uploadScript.error && (
                            <span className="mt-1 block text-small-label text-danger">
                                {uploadScript.error.message}
                            </span>
                        )}
                    </Field>
                    <Field
                        label={t("Jobs_CronExpression")}
                        required
                        error={form.formState.errors.cronExpression?.message}
                    >
                        <div className="flex items-center gap-2">
                            <Input
                                readOnly={readOnly}
                                className="font-mono"
                                {...form.register("cronExpression")}
                            />
                            {!readOnly && (
                                <Button
                                    type="button"
                                    variant="secondary"
                                    className="shrink-0"
                                    onClick={() => setCronBuilderOpen(true)}
                                >
                                    <Wand2 className="h-4 w-4" />
                                    {t("Jobs_CronBuilder_Build")}
                                </Button>
                            )}
                        </div>
                    </Field>
                    <Field
                        label={t("Jobs_RunAsUser")}
                        required
                        error={form.formState.errors.runAsUser?.message}
                    >
                        {runAsUserControl}
                    </Field>
                    <Field
                        label={t("Jobs_MemoryLimitMb")}
                        required
                        error={form.formState.errors.memoryLimitMb?.message}
                    >
                        <Input
                            type="number"
                            min={1}
                            readOnly={readOnly}
                            {...form.register("memoryLimitMb", { valueAsNumber: true })}
                        />
                    </Field>
                    <Field
                        label={t("Jobs_MaxDurationMinutes")}
                        required
                        error={form.formState.errors.maxDurationMinutes?.message}
                    >
                        <Input
                            type="number"
                            min={1}
                            readOnly={readOnly}
                            {...form.register("maxDurationMinutes", { valueAsNumber: true })}
                        />
                    </Field>
                    <Field
                        label={t("Jobs_NotifyEmails")}
                        className="lg:col-span-2"
                        error={form.formState.errors.notifyEmails?.message}
                    >
                        <Input
                            readOnly={readOnly}
                            placeholder="ops@example.com, oncall@example.com"
                            {...form.register("notifyEmails")}
                        />
                    </Field>
                    <Field label={t("Description")} className="lg:col-span-3">
                        <Textarea rows={3} readOnly={readOnly} {...form.register("description")} />
                    </Field>
                </div>
            </Section>
            {detail.data && (
                <Section title={t("Audit")}>
                    <div className="grid grid-cols-2 gap-3 text-dense md:grid-cols-4">
                        <span>
                            {t("Jobs_ApprovalStatus")}
                            <strong className="block">
                                <Badge
                                    variant={JOB_APPROVAL_STATUS_BADGE[detail.data.approvalStatus]}
                                >
                                    {t(JOB_APPROVAL_STATUS_LABELS[detail.data.approvalStatus])}
                                </Badge>
                            </strong>
                        </span>
                        <span>
                            {t("Enabled")}
                            <strong className="block">
                                <Badge variant={detail.data.enabled ? "success" : "neutral"}>
                                    {t(detail.data.enabled ? "Enabled" : "Disabled")}
                                </Badge>
                            </strong>
                        </span>
                        <span>
                            {t("CreatedAt")}
                            <strong className="block">
                                {new Date(detail.data.createdAt).toLocaleString()}
                            </strong>
                        </span>
                        <span>
                            {t("CreatedBy")}
                            <strong className="block">{detail.data.createdBy}</strong>
                        </span>
                        <span>
                            {t("UpdatedAt")}
                            <strong className="block">
                                {detail.data.updatedAt
                                    ? new Date(detail.data.updatedAt).toLocaleString()
                                    : "—"}
                            </strong>
                        </span>
                        <span>
                            {t("Jobs_ApprovedBy")}
                            <strong className="block">{detail.data.approvedBy ?? "—"}</strong>
                        </span>
                        <span>
                            {t("Jobs_ApprovedAt")}
                            <strong className="block">
                                {detail.data.approvedAt
                                    ? new Date(detail.data.approvedAt).toLocaleString()
                                    : "—"}
                            </strong>
                        </span>
                        <span>
                            {t("Jobs_ScriptSha256")}
                            <strong className="block break-all font-mono text-small-label">
                                {detail.data.scriptSha256 ?? "—"}
                            </strong>
                        </span>
                        {detail.data.rejectionReason && (
                            <span className="col-span-2 md:col-span-4">
                                {t("Jobs_RejectionReason")}
                                <strong className="block">{detail.data.rejectionReason}</strong>
                            </span>
                        )}
                    </div>
                </Section>
            )}
            {save.error && (
                <p className="rounded bg-danger-subtle p-3 text-body text-danger">
                    {save.error.message}
                </p>
            )}
            <CronExpressionBuilderDialog
                open={cronBuilderOpen}
                initialValue={String(form.watch("cronExpression") ?? "")}
                onOpenChange={setCronBuilderOpen}
                onApply={(expression) =>
                    form.setValue("cronExpression", expression, {
                        shouldDirty: true,
                        shouldValidate: true,
                    })
                }
            />
        </form>
    );
}
