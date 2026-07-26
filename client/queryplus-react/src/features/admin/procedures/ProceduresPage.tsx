import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Database, Eye, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { apiFetch } from "@/api/client";
import { categoryLookupQuery, proceduresSearch } from "@/api/queries";
import type { ProcedureListItem } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Select } from "@/components/ui/fields";
import { Input } from "@/components/ui/input";
import { Pager } from "@/components/ui/pager";
import { Field, PageHeader } from "@/components/ui/page";

export function ProceduresPage() {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const categories = useQuery(categoryLookupQuery);
    const [draft, setDraft] = useState({
        categoryId: "",
        caption: "",
        roleEntitlement: "",
        enabled: "",
    });
    const [filter, setFilter] = useState(draft);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [deleting, setDeleting] = useState<ProcedureListItem | null>(null);
    const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize) });
    Object.entries(filter).forEach(([key, value]) => value && params.set(key, value));
    const procedures = useQuery({
        queryKey: ["procedures", "search", params.toString()],
        queryFn: () => proceduresSearch(params),
    });
    const remove = useMutation({
        mutationFn: (id: number) => apiFetch<void>(`/api/procedures/${id}`, { method: "DELETE" }),
        onSuccess: async () => {
            setDeleting(null);
            await queryClient.invalidateQueries({ queryKey: ["procedures"] });
        },
    });
    const total = procedures.data?.totalCount ?? 0;
    const updateDraft = (key: keyof typeof draft, value: string) =>
        setDraft((current) => ({ ...current, [key]: value }));
    return (
        <div className="flex min-h-0 flex-1 flex-col gap-3 p-4">
            <PageHeader
                title={t("Procedures_Title")}
                icon={<Database className="h-4 w-4 text-cyan-500" />}
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
                                const empty = {
                                    categoryId: "",
                                    caption: "",
                                    roleEntitlement: "",
                                    enabled: "",
                                };
                                setDraft(empty);
                                setFilter(empty);
                                setPage(1);
                            }}
                        >
                            {t("Clear")}
                        </Button>
                        <Button asChild variant="accent">
                            <Link to="/admin/procedures/new">
                                <Plus className="h-4 w-4" />
                                {t("Create")}
                            </Link>
                        </Button>
                    </>
                }
            />
            <Card>
                <CardHeader>
                    <h2 className="text-card-title font-semibold">{t("Filter")}</h2>
                </CardHeader>
                <CardBody className="grid gap-3 md:grid-cols-6">
                    <Field label={t("Procedures_Category")}>
                        <Select
                            value={draft.categoryId}
                            onChange={(e) => updateDraft("categoryId", e.target.value)}
                        >
                            <option value="">—</option>
                            {categories.data?.map((category) => (
                                <option key={category.id} value={category.id}>
                                    {category.description}
                                </option>
                            ))}
                        </Select>
                    </Field>
                    <Field label={t("Procedures_Caption")}>
                        <Input
                            value={draft.caption}
                            onChange={(e) => updateDraft("caption", e.target.value)}
                        />
                    </Field>
                    <Field label={t("Procedures_Role")}>
                        <Input
                            value={draft.roleEntitlement}
                            onChange={(e) => updateDraft("roleEntitlement", e.target.value)}
                        />
                    </Field>
                    <Field label={t("Enabled")}>
                        <Select
                            value={draft.enabled}
                            onChange={(e) => updateDraft("enabled", e.target.value)}
                        >
                            <option value="">—</option>
                            <option value="true">{t("Yes")}</option>
                            <option value="false">{t("No")}</option>
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
                                <th>{t("Procedures_Caption")}</th>
                                <th>{t("Procedures_Category")}</th>
                                <th>{t("Procedures_Database")}</th>
                                <th>{t("Procedures_Name")}</th>
                                <th>{t("Procedures_Role")}</th>
                                <th>{t("Enabled")}</th>
                                <th>{t("Actions")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {procedures.data?.items.map((procedure) => (
                                <tr key={procedure.id}>
                                    <td className="text-right">{procedure.id}</td>
                                    <td>{procedure.caption}</td>
                                    <td>{procedure.categoryDescription ?? "—"}</td>
                                    <td>{procedure.databaseName}</td>
                                    <td className="font-mono">{procedure.procedureName}</td>
                                    <td>{procedure.roleEntitlement}</td>
                                    <td>
                                        <span
                                            className={procedure.enabled ? "badge-ok" : "badge-off"}
                                        >
                                            {t(procedure.enabled ? "Enabled" : "Disabled")}
                                        </span>
                                    </td>
                                    <td>
                                        <div className="flex justify-center gap-1">
                                            <Button asChild size="sm" variant="ghost">
                                                <Link
                                                    to={`/admin/procedures/${procedure.id}?mode=view`}
                                                >
                                                    <Eye className="h-3 w-3" />
                                                    {t("View")}
                                                </Link>
                                            </Button>
                                            <Button asChild size="sm" variant="ghost">
                                                <Link to={`/admin/procedures/${procedure.id}`}>
                                                    <Pencil className="h-3 w-3" />
                                                    {t("Edit")}
                                                </Link>
                                            </Button>
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                className="text-red-700"
                                                onClick={() => setDeleting(procedure)}
                                            >
                                                <Trash2 className="h-3 w-3" />
                                                {t("Delete")}
                                            </Button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                            {!procedures.data?.items.length && (
                                <tr>
                                    <td colSpan={8} className="p-8 text-center text-slate-500">
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
            <ConfirmDialog
                open={!!deleting}
                title={t("ConfirmDelete")}
                description={deleting?.caption}
                onOpenChange={(open) => !open && setDeleting(null)}
                onConfirm={() => deleting && remove.mutate(deleting.id)}
            />
        </div>
    );
}
