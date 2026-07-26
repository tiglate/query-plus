import { useMutation, useQuery } from "@tanstack/react-query";
import {
    Download,
    Eraser,
    FileSpreadsheet,
    LoaderCircle,
    Play,
    SlidersHorizontal,
    Table2,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import {
    accessibleProceduresQuery,
    execute,
    exportStatus,
    procedureParametersQuery,
    queueExport,
} from "@/api/queries";
import type {
    ExecuteResponse,
    ExportJob,
    ParameterType,
    ProcedureLookup,
    ProcedureParameter,
} from "@/api/types";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Pager } from "@/components/ui/pager";
import { formatTemplate } from "@/lib/utils";
import { MaximizeButton, useResultsMaximize } from "./ResultsMaximize";
import { ResultsGrid } from "./ResultsGrid";

export type ParameterFormValues = Record<string, string | boolean>;

export function parameterInputType(type: ParameterType): string {
    return (
        ["text", "number", "date", "time", "datetime-local", "checkbox", "select"][type] ?? "text"
    );
}

export function serializeParameterValues(
    values: ParameterFormValues,
): Record<string, string | null> {
    return Object.fromEntries(
        Object.entries(values).map(([key, value]) => [
            key,
            typeof value === "boolean" ? String(value) : value || null,
        ]),
    );
}

export function isExportEligible(
    result: ExecuteResponse | null,
    signature: string | null,
    currentSignature: string,
): boolean {
    if (!result?.success || !signature || signature !== currentSignature) return false;
    return result.supportsPagination ? (result.totalRecords ?? 0) > 0 : result.rows.length > 0;
}

function resultSignature(procedureId: number | null, values: ParameterFormValues): string {
    return JSON.stringify([procedureId, serializeParameterValues(values)]);
}

function normalizeExportStatus(
    status: ExportJob["status"],
): "queued" | "running" | "completed" | "failed" {
    if (status === 0) return "queued";
    if (status === 1) return "running";
    if (status === 2) return "completed";
    if (status === 3) return "failed";
    return status;
}

function ParameterControl({
    parameter,
    register,
    error,
}: {
    parameter: ProcedureParameter;
    register: ReturnType<typeof useForm<ParameterFormValues>>["register"];
    error?: string;
}) {
    const required = parameter.isRequired && parameter.parameterType !== 5;
    const name = parameter.name;
    const common = register(name, { required });
    return (
        <div>
            {parameter.parameterType !== 5 && (
                <label
                    className={required ? "label-required" : "label"}
                    htmlFor={`parameter-${name}`}
                >
                    {parameter.caption}
                </label>
            )}
            {parameter.parameterType === 5 ? (
                <label className="mt-2 flex items-center gap-2 text-body font-medium">
                    <input
                        id={`parameter-${name}`}
                        type="checkbox"
                        className="h-4 w-4"
                        {...common}
                    />
                    {parameter.caption}
                </label>
            ) : parameter.parameterType === 6 ? (
                <select
                    id={`parameter-${name}`}
                    className="input"
                    aria-invalid={!!error}
                    {...common}
                >
                    {!required && <option value="">—</option>}
                    {(parameter.comboOptions ?? []).map((option) => (
                        <option key={option} value={option}>
                            {option}
                        </option>
                    ))}
                </select>
            ) : (
                <Input
                    id={`parameter-${name}`}
                    type={parameterInputType(parameter.parameterType)}
                    step={parameter.parameterType === 1 ? "any" : undefined}
                    aria-invalid={!!error}
                    {...common}
                />
            )}
            {error && <p className="mt-1 text-small-label text-red-700">{error}</p>}
        </div>
    );
}

function ProcedureList({
    procedures,
    selectedId,
    onSelect,
}: {
    procedures: ProcedureLookup[];
    selectedId: number | null;
    onSelect: (procedure: ProcedureLookup) => void;
}) {
    const { t } = useTranslation();
    const groups = useMemo(() => {
        const map = new Map<string, ProcedureLookup[]>();
        procedures.forEach((procedure) => {
            const key = procedure.categoryDescription?.trim() || t("Home_Uncategorized");
            map.set(key, [...(map.get(key) ?? []), procedure]);
        });
        return [...map].sort(([a], [b]) => a.localeCompare(b));
    }, [procedures, t]);
    return (
        <div
            className="min-h-0 flex-1 overflow-auto"
            role="listbox"
            aria-label={t("Home_SelectProcedure")}
        >
            {groups.length === 0 && (
                <p className="p-3 text-small-label text-slate-500">{t("NoRecords")}</p>
            )}
            {groups.map(([category, items]) => (
                <div key={category} role="group" aria-label={category}>
                    <div className="sticky top-0 z-10 flex justify-between bg-slate-100 px-3 py-1.5 text-caption font-semibold uppercase tracking-wide text-slate-500 dark:bg-navy-900">
                        <span>{category}</span>
                        <span>{items.length}</span>
                    </div>
                    {items.map((procedure) => (
                        <button
                            type="button"
                            role="option"
                            aria-selected={selectedId === procedure.id}
                            key={procedure.id}
                            className={`block w-full border-b border-slate-100 px-3 py-2 text-left hover:bg-cyan-50 dark:border-navy-700 dark:hover:bg-navy-700 ${selectedId === procedure.id ? "border-l-4 border-l-cyan-500 bg-cyan-50 dark:bg-navy-700" : ""}`}
                            onClick={() => onSelect(procedure)}
                        >
                            <span className="block truncate text-body font-medium">
                                {procedure.caption}
                            </span>
                            <span className="block truncate text-small-label text-slate-500">
                                {procedure.description || "—"}
                            </span>
                        </button>
                    ))}
                </div>
            ))}
        </div>
    );
}

export function HomePage() {
    const { t } = useTranslation();
    const procedures = useQuery(accessibleProceduresQuery);
    const [selected, setSelected] = useState<ProcedureLookup | null>(null);
    const parameters = useQuery(procedureParametersQuery(selected?.id ?? 0));
    const form = useForm<ParameterFormValues>();
    const values = form.watch();
    const currentSignature = resultSignature(selected?.id ?? null, values);
    const [result, setResult] = useState<ExecuteResponse | null>(null);
    const [executedSignature, setExecutedSignature] = useState<string | null>(null);
    const [jobId, setJobId] = useState<string | null>(null);
    const { maximized, toggle } = useResultsMaximize();

    useEffect(() => {
        if (!parameters.data) return;
        form.reset(
            Object.fromEntries(
                parameters.data.map((parameter) => [
                    parameter.name,
                    parameter.parameterType === 5
                        ? parameter.defaultValue?.toLowerCase() === "true"
                        : (parameter.defaultValue ?? ""),
                ]),
            ),
        );
    }, [form, parameters.data]);

    const executeMutation = useMutation({
        mutationFn: ({
            page,
            signature,
            parameterValues,
        }: {
            page: number;
            signature: string;
            parameterValues: ParameterFormValues;
        }) =>
            execute({
                procedureId: selected!.id,
                parameterValues: serializeParameterValues(parameterValues),
                pageNumber: page,
                pageSize: 50,
            }).then((response) => ({ response, signature })),
        onSuccess: ({ response, signature }) => {
            setResult(response);
            setExecutedSignature(response.success ? signature : null);
            setJobId(null);
        },
    });
    const exportMutation = useMutation({
        mutationFn: ({ parameterValues }: { parameterValues: ParameterFormValues }) =>
            queueExport({
                procedureId: selected!.id,
                parameterValues: serializeParameterValues(parameterValues),
            }),
        onSuccess: (job) => setJobId(job.jobId ?? job.id),
    });
    const status = useQuery({
        queryKey: ["export", jobId],
        queryFn: () => exportStatus(jobId!),
        enabled: !!jobId,
        refetchInterval: (query) => {
            const state = query.state.data
                ? normalizeExportStatus(query.state.data.status)
                : "queued";
            return state === "completed" || state === "failed" ? false : 1500;
        },
    });
    const exportReady = isExportEligible(result, executedSignature, currentSignature);
    const total = result?.supportsPagination
        ? (result.totalRecords ?? 0)
        : (result?.rows.length ?? 0);

    const run = form.handleSubmit(
        () => {
            if (!selected) return;
            const values = form.getValues();
            executeMutation.mutate({
                page: 1,
                signature: resultSignature(selected.id, values),
                parameterValues: values,
            });
        },
        (errors) => {
            const captions = (parameters.data ?? [])
                .filter((p) => errors[p.name])
                .map((p) => p.caption);
            if (captions.length)
                setResult({
                    success: false,
                    errorMessage: formatTemplate(
                        t("Home_RequiredParametersMissing"),
                        captions.join(", "),
                    ),
                    procedureId: selected?.id ?? 0,
                    rowCount: 0,
                    supportsPagination: false,
                    pageNumber: 1,
                    pageSize: 50,
                    totalRecords: 0,
                    columns: [],
                    rows: [],
                });
        },
    );
    const goPage = (page: number) => {
        if (!selected) return;
        const values = form.getValues();
        executeMutation.mutate({
            page,
            signature: resultSignature(selected.id, values),
            parameterValues: values,
        });
    };
    const clear = () => {
        setSelected(null);
        setResult(null);
        setExecutedSignature(null);
        setJobId(null);
        form.reset({});
    };
    const exportState = status.data ? normalizeExportStatus(status.data.status) : null;

    return (
        <div className="flex min-h-0 flex-1 flex-col gap-3 p-3 lg:p-4">
            <Card className="shrink-0">
                <CardHeader>
                    <h1 className="flex items-center gap-2 text-page-title font-semibold">
                        <Play className="h-4 w-4 text-cyan-500" />
                        {t("Home_Title")}
                    </h1>
                    <div className="flex flex-wrap gap-2">
                        <Button
                            id="btn-execute"
                            disabled={!selected || executeMutation.isPending}
                            onClick={() => void run()}
                        >
                            {executeMutation.isPending ? (
                                <LoaderCircle className="h-4 w-4 animate-spin" />
                            ) : (
                                <Play className="h-4 w-4" />
                            )}
                            {t("Home_Execute")}
                        </Button>
                        <Button variant="secondary" disabled={!selected} onClick={clear}>
                            <Eraser className="h-4 w-4" />
                            {t("Clear")}
                        </Button>
                        <Button
                            id="btn-export"
                            variant="accent"
                            disabled={!exportReady || exportMutation.isPending}
                            title={exportReady ? t("Home_Export") : t("Home_ExportRequiresData")}
                            onClick={() =>
                                exportMutation.mutate({ parameterValues: form.getValues() })
                            }
                        >
                            <FileSpreadsheet className="h-4 w-4" />
                            {t("Home_Export")}
                        </Button>
                    </div>
                </CardHeader>
            </Card>
            <form
                className={`grid min-h-0 flex-1 gap-3 ${maximized ? "grid-cols-1" : "grid-cols-1 lg:grid-cols-[minmax(13rem,20%)_minmax(12rem,18%)_1fr]"}`}
                onSubmit={(event) => {
                    event.preventDefault();
                    void run();
                }}
            >
                {!maximized && (
                    <Card className="flex min-h-48 flex-col overflow-hidden">
                        <CardHeader>
                            <h2 className="text-card-title font-semibold">
                                {t("Home_SelectProcedure")}
                            </h2>
                            <span className="text-small-label text-slate-500">
                                {procedures.data?.length ?? 0}
                            </span>
                        </CardHeader>
                        <ProcedureList
                            procedures={procedures.data ?? []}
                            selectedId={selected?.id ?? null}
                            onSelect={(procedure) => {
                                setSelected(procedure);
                                setResult(null);
                                setExecutedSignature(null);
                                setJobId(null);
                            }}
                        />
                    </Card>
                )}
                {!maximized && (
                    <Card className="flex min-h-48 flex-col overflow-hidden">
                        <CardHeader>
                            <h2 className="flex items-center gap-2 text-card-title font-semibold">
                                <SlidersHorizontal className="h-4 w-4" />
                                {t("Home_Parameters")}
                            </h2>
                        </CardHeader>
                        <CardBody className="min-h-0 flex-1 space-y-3 overflow-auto">
                            {!selected && (
                                <p className="text-small-label text-slate-500">
                                    {t("Home_NoProcedure")}
                                </p>
                            )}
                            {parameters.data?.map((parameter) => (
                                <ParameterControl
                                    key={parameter.id ?? parameter.name}
                                    parameter={parameter}
                                    register={form.register}
                                    error={
                                        form.formState.errors[parameter.name]
                                            ? formatTemplate(
                                                  t("Home_RequiredParametersClient"),
                                                  parameter.caption,
                                              )
                                            : undefined
                                    }
                                />
                            ))}
                        </CardBody>
                    </Card>
                )}
                <Card className="flex min-h-64 flex-col overflow-hidden">
                    <CardHeader>
                        <h2 className="flex items-center gap-2 text-card-title font-semibold">
                            <Table2 className="h-4 w-4" />
                            {t("Home_Results")}
                        </h2>
                        <div className="flex items-center gap-2">
                            {jobId &&
                                exportState &&
                                (exportState === "completed" ? (
                                    <a
                                        className="inline-flex items-center gap-1 text-small-label font-medium text-cyan-700 underline dark:text-cyan-400"
                                        href={`/api/exports/${jobId}/download`}
                                    >
                                        <Download className="h-3 w-3" />
                                        {t("Home_Download")}
                                    </a>
                                ) : (
                                    <span
                                        className={
                                            exportState === "failed"
                                                ? "text-small-label text-red-700"
                                                : "text-small-label text-slate-500"
                                        }
                                    >
                                        {t(
                                            exportState === "failed"
                                                ? "Home_ExportFailed"
                                                : "Home_ExportRunning",
                                        )}
                                    </span>
                                ))}
                            <MaximizeButton maximized={maximized} onToggle={toggle} />
                        </div>
                    </CardHeader>
                    <div className="flex min-h-0 flex-1 flex-col">
                        {(executeMutation.error || exportMutation.error) && (
                            <div className="m-3 rounded border border-red-200 bg-red-50 p-2 text-small-label text-red-800">
                                {(executeMutation.error ?? exportMutation.error)?.message}
                            </div>
                        )}
                        {result?.errorMessage && (
                            <div className="m-3 rounded border border-red-200 bg-red-50 p-2 text-small-label text-red-800">
                                {result.errorMessage}
                            </div>
                        )}
                        {result?.success && result.rows.length > 0 ? (
                            <ResultsGrid
                                columns={result.columns}
                                rows={result.rows}
                                meta={`${result.rowCount} ${t("Home_Rows")}`}
                            />
                        ) : (
                            !result?.errorMessage && (
                                <p className="p-4 text-small-label text-slate-500">
                                    {t("NoRecords")}
                                </p>
                            )
                        )}
                    </div>
                    {result?.success && result.supportsPagination && total > 0 && (
                        <Pager
                            page={result.pageNumber}
                            pageSize={result.pageSize}
                            total={total}
                            onPage={goPage}
                        />
                    )}
                </Card>
            </form>
        </div>
    );
}
