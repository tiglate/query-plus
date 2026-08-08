import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    ArrowLeft,
    Database,
    ListChecks,
    ListOrdered,
    Plus,
    Power,
    RotateCw,
    Save,
    SlidersHorizontal,
    Trash2,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { z } from "zod";
import { apiFetch } from "@/api/client";
import { categoryLookupQuery, connectionsLookupQuery, procedureQuery } from "@/api/queries";
import type { ProcedureDetail, ProcedureInput } from "@/api/types";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { PageHeader, Field, Section } from "@/components/ui/page";
import { Select, Textarea } from "@/components/ui/fields";
import { Switch } from "@/components/ui/switch";
import { ComboValuesEditorDialog } from "./ComboValuesEditorDialog";

const parameterSchema = z.object({
    id: z.number().optional(),
    caption: z.string().trim().min(1),
    name: z.string().trim().min(1),
    parameterType: z.coerce.number().int().min(0).max(6),
    defaultValue: z.string().nullable(),
    comboValues: z.string().nullable(),
    isRequired: z.boolean(),
});
const columnSchema = z.object({
    id: z.number().optional(),
    technicalName: z.string().trim().min(1),
    caption: z.string().trim().min(1),
    alignment: z.coerce.number().int().min(0).max(2),
    formatMask: z.string().nullable(),
    visible: z.boolean(),
});
const procedureSchema = z
    .object({
        categoryId: z.coerce.number().int().positive(),
        caption: z.string().trim().min(1).max(300),
        connectionName: z.string().trim().min(1),
        databaseName: z.string().trim().min(1),
        procedureName: z.string().trim().min(1),
        roleEntitlement: z.string().trim().min(1),
        enabled: z.boolean(),
        supportsPagination: z.boolean(),
        description: z.string().nullable(),
        parameters: z.array(parameterSchema),
        columns: z.array(columnSchema),
    })
    .superRefine((value, context) => {
        value.parameters.forEach((parameter, index) => {
            if (parameter.parameterType !== 6) return;
            try {
                const parsed: unknown = JSON.parse(parameter.comboValues || "[]");
                if (!Array.isArray(parsed) || parsed.some((item) => typeof item !== "string")) {
                    throw new Error("Invalid comboValues");
                }
            } catch {
                context.addIssue({
                    code: "custom",
                    path: ["parameters", index, "comboValues"],
                    message: "JSON array of strings required",
                });
            }
        });
    });

export type ProcedureFormValues = z.input<typeof procedureSchema>;

export function procedureFormToApi(values: ProcedureFormValues, id?: number): ProcedureInput {
    const parsed = procedureSchema.parse(values);
    return {
        id,
        categoryId: parsed.categoryId,
        caption: parsed.caption.trim(),
        connectionName: parsed.connectionName.trim(),
        databaseName: parsed.databaseName.trim(),
        procedureName: parsed.procedureName.trim(),
        roleEntitlement: parsed.roleEntitlement.trim(),
        enabled: parsed.enabled,
        supportsPagination: parsed.supportsPagination,
        description: parsed.description?.trim() || null,
        parameters: parsed.parameters.map((parameter) => ({
            ...parameter,
            caption: parameter.caption.trim(),
            name: parameter.name.trim(),
            parameterType: parameter.parameterType as 0 | 1 | 2 | 3 | 4 | 5 | 6,
            defaultValue: parameter.defaultValue?.trim() || null,
            comboValues:
                parameter.parameterType === 6
                    ? JSON.stringify(JSON.parse(parameter.comboValues || "[]"))
                    : null,
        })),
        columns: parsed.columns.map((column) => ({
            ...column,
            technicalName: column.technicalName.trim(),
            caption: column.caption.trim(),
            alignment: column.alignment as 0 | 1 | 2,
            formatMask: column.formatMask?.trim() || null,
        })),
    };
}

const defaults: ProcedureFormValues = {
    categoryId: 0,
    caption: "",
    connectionName: "",
    databaseName: "",
    procedureName: "",
    roleEntitlement: "",
    enabled: true,
    supportsPagination: false,
    description: "",
    parameters: [],
    columns: [],
};

function detailToForm(detail: ProcedureDetail): ProcedureFormValues {
    return {
        categoryId: detail.categoryId,
        caption: detail.caption,
        connectionName: detail.connectionName,
        databaseName: detail.databaseName,
        procedureName: detail.procedureName,
        roleEntitlement: detail.roleEntitlement,
        enabled: detail.enabled,
        supportsPagination: detail.supportsPagination,
        description: detail.description ?? "",
        parameters: detail.parameters.map((parameter) => ({
            ...parameter,
            defaultValue: parameter.defaultValue ?? "",
            comboValues: parameter.comboValues ?? "",
        })),
        columns: detail.columns.map((column) => ({
            ...column,
            formatMask: column.formatMask ?? "",
        })),
    };
}

export function ProcedureEditorPage() {
    const { t } = useTranslation();
    const route = useParams();
    const [search] = useSearchParams();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const id = route.id && route.id !== "new" ? Number(route.id) : undefined;
    const readOnly = search.get("mode") === "view";
    const detail = useQuery(procedureQuery(id ?? 0));
    const categories = useQuery(categoryLookupQuery);
    const connections = useQuery(connectionsLookupQuery);
    const form = useForm<ProcedureFormValues>({
        resolver: zodResolver(procedureSchema),
        defaultValues: defaults,
    });
    const parameters = useFieldArray({
        control: form.control,
        name: "parameters",
        keyName: "_key",
    });
    const columns = useFieldArray({ control: form.control, name: "columns", keyName: "_key" });
    const [confirmSync, setConfirmSync] = useState(false);
    const [comboEditorIndex, setComboEditorIndex] = useState<number | null>(null);
    const connectionName = form.watch("connectionName");
    const databaseName = form.watch("databaseName");
    const procedureName = form.watch("procedureName");

    useEffect(() => {
        form.reset(detail.data ? detailToForm(detail.data) : defaults);
    }, [detail.data, form, id]);

    const save = useMutation({
        mutationFn: (values: ProcedureFormValues) =>
            apiFetch<ProcedureDetail>(id ? `/api/procedures/${id}` : "/api/procedures", {
                method: id ? "PUT" : "POST",
                body: JSON.stringify(procedureFormToApi(values, id)),
            }),
        onSuccess: async (saved) => {
            await queryClient.invalidateQueries({ queryKey: ["procedures"] });
            navigate(`/admin/procedures/${saved.id}`);
        },
    });
    const sync = useMutation({
        mutationFn: () =>
            apiFetch<Partial<ProcedureDetail>>(`/api/procedures/${id ?? 0}/sync-metadata`, {
                method: "POST",
                body: JSON.stringify({
                    connectionName: String(connectionName).trim(),
                    databaseName: String(databaseName).trim(),
                    procedureName: String(procedureName).trim(),
                }),
            }),
        onSuccess: (response) => {
            if (response.parameters) form.setValue("parameters", response.parameters);
            if (response.columns) form.setValue("columns", response.columns);
            setConfirmSync(false);
        },
    });
    const parameterTypeKeys = [
        "ParamType_FreeText",
        "ParamType_Numeric",
        "ParamType_Date",
        "ParamType_Time",
        "ParamType_DateTime",
        "ParamType_Boolean",
        "ParamType_Combo",
    ];
    const alignmentKeys = ["Alignment_Left", "Alignment_Center", "Alignment_Right"];
    let pageTitle;
    if (readOnly) {
        pageTitle = t("Procedures_View");
    } else if (id) {
        pageTitle = t("Procedures_Edit");
    } else {
        pageTitle = t("Procedures_New");
    }
    return (
        <form
            className="space-y-4 p-4"
            onSubmit={form.handleSubmit((values) => save.mutate(values))}
        >
            <PageHeader
                title={pageTitle}
                icon={<Database className="h-4 w-4 text-cyan-500" />}
                actions={
                    <>
                        <Button asChild type="button" variant="secondary">
                            <Link to="/admin/procedures">
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
            <Section
                title={t("Procedures_Title")}
                actions={
                    !readOnly && (
                        <Button
                            type="button"
                            variant="accent"
                            disabled={
                                !String(connectionName).trim() ||
                                !String(databaseName).trim() ||
                                !String(procedureName).trim()
                            }
                            onClick={() => setConfirmSync(true)}
                        >
                            <RotateCw className="h-4 w-4" />
                            {t("Procedures_SyncMetadata")}
                        </Button>
                    )
                }
            >
                <div className="grid gap-4 lg:grid-cols-3">
                    <Field
                        label={t("Procedures_Category")}
                        required
                        error={form.formState.errors.categoryId?.message}
                    >
                        <Select
                            disabled={readOnly}
                            {...form.register("categoryId", { valueAsNumber: true })}
                        >
                            <option value={0}>{t("Procedures_SelectCategory")}</option>
                            {categories.data?.map((category) => (
                                <option key={category.id} value={category.id}>
                                    {category.description}
                                </option>
                            ))}
                        </Select>
                    </Field>
                    <Field
                        label={t("Procedures_Caption")}
                        required
                        error={form.formState.errors.caption?.message}
                    >
                        <Input readOnly={readOnly} {...form.register("caption")} />
                    </Field>
                    <Field
                        label={t("Procedures_Server")}
                        required
                        error={form.formState.errors.connectionName?.message}
                    >
                        <Select disabled={readOnly} {...form.register("connectionName")}>
                            <option value="">{t("Procedures_SelectServer")}</option>
                            {connections.data?.map((name) => (
                                <option key={name} value={name}>
                                    {name}
                                </option>
                            ))}
                        </Select>
                    </Field>
                    <Field
                        label={t("Procedures_Database")}
                        required
                        error={form.formState.errors.databaseName?.message}
                    >
                        <Input
                            readOnly={readOnly}
                            className="font-mono"
                            {...form.register("databaseName")}
                        />
                    </Field>
                    <Field
                        label={t("Procedures_Name")}
                        required
                        error={form.formState.errors.procedureName?.message}
                    >
                        <Input
                            readOnly={readOnly}
                            className="font-mono"
                            {...form.register("procedureName")}
                        />
                    </Field>
                    <Field
                        label={t("Procedures_Role")}
                        required
                        error={form.formState.errors.roleEntitlement?.message}
                    >
                        <Input readOnly={readOnly} {...form.register("roleEntitlement")} />
                    </Field>
                    <Field label={t("Description")} className="lg:col-span-2">
                        <Textarea rows={3} readOnly={readOnly} {...form.register("description")} />
                    </Field>
                    <div>
                        <span className="flex items-center gap-1.5 text-small-label font-medium text-slate-700 dark:text-slate-200">
                            <SlidersHorizontal className="h-3.5 w-3.5" />
                            {t("Settings")}
                        </span>
                        <div className="mt-1 divide-y divide-slate-200 rounded-md border border-slate-300 bg-white dark:divide-navy-700 dark:border-navy-600 dark:bg-navy-900">
                            <label className="flex h-9 items-center justify-between gap-3 px-3">
                                <span className="flex items-center gap-2 text-body text-slate-700 dark:text-slate-200">
                                    <Power className="h-4 w-4 text-cyan-500" />
                                    {t("Enabled")}
                                </span>
                                <Switch disabled={readOnly} {...form.register("enabled")} />
                            </label>
                            <label className="flex h-9 items-center justify-between gap-3 px-3">
                                <span className="flex items-center gap-2 text-body text-slate-700 dark:text-slate-200">
                                    <ListOrdered className="h-4 w-4 text-cyan-500" />
                                    {t("Procedures_SupportsPagination")}
                                </span>
                                <Switch
                                    disabled={readOnly}
                                    {...form.register("supportsPagination")}
                                />
                            </label>
                        </div>
                    </div>
                </div>
            </Section>
            <Section
                title={t("Procedures_Parameters")}
                actions={
                    !readOnly && (
                        <Button
                            type="button"
                            variant="secondary"
                            onClick={() =>
                                parameters.append({
                                    caption: "Param",
                                    name: "@Param",
                                    parameterType: 0,
                                    defaultValue: "",
                                    comboValues: "",
                                    isRequired: false,
                                })
                            }
                        >
                            <Plus className="h-4 w-4" />
                            {t("Procedures_AddParameter")}
                        </Button>
                    )
                }
            >
                <div className="overflow-x-auto">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>{t("Param_Caption")}</th>
                                <th>{t("Param_Name")}</th>
                                <th>{t("Param_Type")}</th>
                                <th>{t("Param_Required")}</th>
                                <th>{t("Param_Default")}</th>
                                <th>{t("Param_Combo")}</th>
                                {!readOnly && <th />}
                            </tr>
                        </thead>
                        <tbody>
                            {parameters.fields.map((parameter, index) => {
                                const type = form.watch(`parameters.${index}.parameterType`);
                                return (
                                    <tr key={parameter._key}>
                                        <td>
                                            <Input
                                                readOnly={readOnly}
                                                {...form.register(`parameters.${index}.caption`)}
                                            />
                                        </td>
                                        <td>
                                            <Input
                                                readOnly={readOnly}
                                                className="font-mono"
                                                {...form.register(`parameters.${index}.name`)}
                                            />
                                        </td>
                                        <td>
                                            <Select
                                                disabled={readOnly}
                                                {...form.register(
                                                    `parameters.${index}.parameterType`,
                                                    { valueAsNumber: true },
                                                )}
                                            >
                                                {parameterTypeKeys.map((key, value) => (
                                                    <option key={key} value={value}>
                                                        {t(key)}
                                                    </option>
                                                ))}
                                            </Select>
                                        </td>
                                        <td className="text-center">
                                            <Switch
                                                disabled={readOnly}
                                                {...form.register(`parameters.${index}.isRequired`)}
                                            />
                                        </td>
                                        <td>
                                            <Input
                                                readOnly={readOnly}
                                                {...form.register(
                                                    `parameters.${index}.defaultValue`,
                                                )}
                                            />
                                        </td>
                                        <td>
                                            {Number(type) === 6 && (
                                                <div>
                                                    <div className="flex items-center gap-2">
                                                        <Input
                                                            readOnly={readOnly}
                                                            className="font-mono"
                                                            placeholder='["A","B"]'
                                                            {...form.register(
                                                                `parameters.${index}.comboValues`,
                                                            )}
                                                        />
                                                        <Button
                                                            type="button"
                                                            variant="secondary"
                                                            size="icon"
                                                            className="shrink-0"
                                                            title={t(
                                                                readOnly
                                                                    ? "Procedures_ComboValues_View"
                                                                    : "Procedures_ComboValues_Edit",
                                                            )}
                                                            aria-label={t(
                                                                readOnly
                                                                    ? "Procedures_ComboValues_View"
                                                                    : "Procedures_ComboValues_Edit",
                                                            )}
                                                            onClick={() =>
                                                                setComboEditorIndex(index)
                                                            }
                                                        >
                                                            <ListChecks className="h-4 w-4" />
                                                        </Button>
                                                    </div>
                                                    {form.formState.errors.parameters?.[index]
                                                        ?.comboValues?.message && (
                                                        <span className="text-small-label text-danger">
                                                            {
                                                                form.formState.errors.parameters[
                                                                    index
                                                                ]?.comboValues?.message
                                                            }
                                                        </span>
                                                    )}
                                                </div>
                                            )}
                                        </td>
                                        {!readOnly && (
                                            <td>
                                                <Button
                                                    type="button"
                                                    variant="ghost"
                                                    size="icon"
                                                    className="text-danger"
                                                    onClick={() => parameters.remove(index)}
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </Button>
                                            </td>
                                        )}
                                    </tr>
                                );
                            })}
                            {parameters.fields.length === 0 && (
                                <tr>
                                    <td colSpan={7} className="p-6 text-center text-muted">
                                        {t("NoRecords")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </Section>
            <Section
                title={t("Procedures_Columns")}
                actions={
                    !readOnly && (
                        <Button
                            type="button"
                            variant="secondary"
                            onClick={() =>
                                columns.append({
                                    technicalName: "Column1",
                                    caption: "Column1",
                                    alignment: 0,
                                    formatMask: "",
                                    visible: true,
                                })
                            }
                        >
                            <Plus className="h-4 w-4" />
                            {t("Procedures_AddColumn")}
                        </Button>
                    )
                }
            >
                <div className="overflow-x-auto">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>{t("Col_Technical")}</th>
                                <th>{t("Col_Caption")}</th>
                                <th>{t("Col_Alignment")}</th>
                                <th>{t("Col_Format")}</th>
                                <th>{t("Col_Visible")}</th>
                                {!readOnly && <th />}
                            </tr>
                        </thead>
                        <tbody>
                            {columns.fields.map((column, index) => (
                                <tr key={column._key}>
                                    <td>
                                        <Input
                                            readOnly={readOnly}
                                            className="font-mono"
                                            {...form.register(`columns.${index}.technicalName`)}
                                        />
                                    </td>
                                    <td>
                                        <Input
                                            readOnly={readOnly}
                                            {...form.register(`columns.${index}.caption`)}
                                        />
                                    </td>
                                    <td>
                                        <Select
                                            disabled={readOnly}
                                            {...form.register(`columns.${index}.alignment`, {
                                                valueAsNumber: true,
                                            })}
                                        >
                                            {alignmentKeys.map((key, value) => (
                                                <option key={key} value={value}>
                                                    {t(key)}
                                                </option>
                                            ))}
                                        </Select>
                                    </td>
                                    <td>
                                        <Input
                                            readOnly={readOnly}
                                            className="font-mono"
                                            {...form.register(`columns.${index}.formatMask`)}
                                        />
                                    </td>
                                    <td className="text-center">
                                        <Switch
                                            disabled={readOnly}
                                            {...form.register(`columns.${index}.visible`)}
                                        />
                                    </td>
                                    {!readOnly && (
                                        <td>
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="icon"
                                                className="text-danger"
                                                onClick={() => columns.remove(index)}
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </Button>
                                        </td>
                                    )}
                                </tr>
                            ))}
                            {columns.fields.length === 0 && (
                                <tr>
                                    <td colSpan={6} className="p-6 text-center text-muted">
                                        {t("NoRecords")}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </Section>
            {detail.data && (
                <Section title={t("Audit")}>
                    <div className="grid grid-cols-2 gap-3 text-dense md:grid-cols-4">
                        <span>
                            {t("CreatedAt")}
                            <strong className="block">
                                {new Date(detail.data.createdAt).toLocaleString()}
                            </strong>
                        </span>
                        <span>
                            {t("CreatedBy")}
                            <strong className="block">{detail.data.createdBy ?? "—"}</strong>
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
                            {t("UpdatedBy")}
                            <strong className="block">{detail.data.updatedBy ?? "—"}</strong>
                        </span>
                    </div>
                </Section>
            )}
            {(save.error || sync.error) && (
                <p className="rounded bg-danger-subtle p-3 text-body text-danger">
                    {(save.error ?? sync.error)?.message}
                </p>
            )}
            <ConfirmDialog
                open={confirmSync}
                title={t("Procedures_SyncMetadata")}
                description={`${connectionName}: ${databaseName}.${procedureName}`}
                onOpenChange={setConfirmSync}
                onConfirm={() => sync.mutate()}
            />
            {comboEditorIndex !== null && (
                <ComboValuesEditorDialog
                    open={comboEditorIndex !== null}
                    readOnly={readOnly}
                    initialJson={form.watch(`parameters.${comboEditorIndex}.comboValues`)}
                    onOpenChange={(open) => !open && setComboEditorIndex(null)}
                    onSave={(json) =>
                        form.setValue(`parameters.${comboEditorIndex}.comboValues`, json, {
                            shouldDirty: true,
                            shouldValidate: true,
                        })
                    }
                />
            )}
        </form>
    );
}
