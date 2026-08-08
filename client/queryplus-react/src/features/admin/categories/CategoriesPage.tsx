import { zodResolver } from "@hookform/resolvers/zod";
import * as Dialog from "@radix-ui/react-dialog";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, Folder, Pencil, Plus, Search, Trash2, X } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { apiFetch } from "@/api/client";
import { categoriesSearch, categoryQuery } from "@/api/queries";
import type { CategoryDetail, CategoryInput } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Pager } from "@/components/ui/pager";
import { Field, PageHeader } from "@/components/ui/page";
import { Select } from "@/components/ui/fields";
import { useAdminSearch } from "../hooks/useAdminSearch";

const categorySchema = z.object({ description: z.string().trim().min(1).max(200) });
type CategoryForm = z.infer<typeof categorySchema>;

export function categoryFormToApi(values: CategoryForm, id?: number): CategoryInput {
    return { id, description: values.description.trim() };
}

function CategoryDialog({
    id,
    mode,
    open,
    onOpenChange,
}: Readonly<{
    id: number | null;
    mode: "create" | "view" | "edit";
    open: boolean;
    onOpenChange: (open: boolean) => void;
}>) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const detail = useQuery(categoryQuery(id ?? 0));
    const form = useForm<CategoryForm>({
        resolver: zodResolver(categorySchema),
        defaultValues: { description: "" },
    });
    useEffect(() => {
        form.reset({ description: detail.data?.description ?? "" });
    }, [detail.data, form, open]);
    const save = useMutation({
        mutationFn: (values: CategoryForm) =>
            apiFetch<CategoryDetail>(id ? `/api/categories/${id}` : "/api/categories", {
                method: id ? "PUT" : "POST",
                body: JSON.stringify(categoryFormToApi(values, id ?? undefined)),
            }),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["categories"] });
            onOpenChange(false);
        },
    });
    const readOnly = mode === "view";
    let titleKey = "Categories_View";
    switch (mode) {
        case "create":
            titleKey = "Categories_New";
            break;
        case "edit":
            titleKey = "Categories_Edit";
            break;
    }
    const title = t(titleKey);
    return (
        <Dialog.Root open={open} onOpenChange={onOpenChange}>
            <Dialog.Portal>
                <Dialog.Overlay className="fixed inset-0 z-50 bg-black/45" />
                <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-[min(36rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white p-5 shadow-xl dark:bg-navy-800">
                    <div className="flex items-center justify-between">
                        <Dialog.Title className="text-card-title font-semibold">
                            {title}
                        </Dialog.Title>
                        <Dialog.Close asChild>
                            <Button variant="ghost" size="icon">
                                <X className="h-4 w-4" />
                            </Button>
                        </Dialog.Close>
                    </div>
                    <form
                        className="mt-5 space-y-4"
                        onSubmit={form.handleSubmit((values) => save.mutate(values))}
                    >
                        {id && (
                            <Field label={t("Id")}>
                                <Input value={id} readOnly />
                            </Field>
                        )}
                        <Field
                            label={t("Description")}
                            required
                            error={form.formState.errors.description?.message}
                        >
                            <Input readOnly={readOnly} {...form.register("description")} />
                        </Field>
                        {detail.data && id && (
                            <div className="grid grid-cols-2 gap-3 rounded bg-slate-50 p-3 text-dense dark:bg-navy-900">
                                <span>
                                    {t("CreatedAt")}
                                    <strong className="block">
                                        {new Date(detail.data.createdAt).toLocaleString()}
                                    </strong>
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
                                    {t("CreatedBy")}
                                    <strong className="block">
                                        {detail.data.createdBy ?? "—"}
                                    </strong>
                                </span>
                                <span>
                                    {t("UpdatedBy")}
                                    <strong className="block">
                                        {detail.data.updatedBy ?? "—"}
                                    </strong>
                                </span>
                            </div>
                        )}
                        {save.error && (
                            <p className="text-body text-danger">{save.error.message}</p>
                        )}
                        <div className="flex justify-end gap-2">
                            <Dialog.Close asChild>
                                <Button type="button" variant="secondary">
                                    {t(readOnly ? "Back" : "Cancel")}
                                </Button>
                            </Dialog.Close>
                            {!readOnly && (
                                <Button type="submit" disabled={save.isPending}>
                                    {t("Save")}
                                </Button>
                            )}
                        </div>
                    </form>
                </Dialog.Content>
            </Dialog.Portal>
        </Dialog.Root>
    );
}

const EMPTY_CATEGORY_FILTERS = { description: "" };

export function CategoriesPage() {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const search = useAdminSearch("categories", EMPTY_CATEGORY_FILTERS, categoriesSearch);
    const [dialog, setDialog] = useState<{
        id: number | null;
        mode: "create" | "view" | "edit";
    } | null>(null);
    const [deleting, setDeleting] = useState<number | null>(null);
    const remove = useMutation({
        mutationFn: (id: number) => apiFetch<void>(`/api/categories/${id}`, { method: "DELETE" }),
        onSuccess: async () => {
            setDeleting(null);
            await queryClient.invalidateQueries({ queryKey: ["categories"] });
        },
    });
    const categories = search.query;
    return (
        <div className="flex min-h-0 flex-1 flex-col gap-3 p-4">
            <PageHeader
                title={t("Categories_Title")}
                icon={<Folder className="h-4 w-4 text-cyan-500" />}
                actions={
                    <>
                        <Button onClick={search.search}>
                            <Search className="h-4 w-4" />
                            {t("Search")}
                        </Button>
                        <Button variant="secondary" onClick={search.clear}>
                            {t("Clear")}
                        </Button>
                        <Button
                            variant="accent"
                            onClick={() => setDialog({ id: null, mode: "create" })}
                        >
                            <Plus className="h-4 w-4" />
                            {t("Create")}
                        </Button>
                    </>
                }
            />
            <Card>
                <CardHeader>
                    <h2 className="text-card-title font-semibold">{t("Filter")}</h2>
                </CardHeader>
                <CardBody className="grid gap-3 sm:grid-cols-[1fr_10rem]">
                    <Field label={t("Description")}>
                        <Input
                            value={search.draft.description}
                            onChange={(event) =>
                                search.updateDraft("description", event.target.value)
                            }
                            onKeyDown={(event) => {
                                if (event.key === "Enter") search.search();
                            }}
                        />
                    </Field>
                    <Field label={t("Pagination_PageSize")}>
                        <Select
                            value={search.pageSize}
                            onChange={(event) => search.changePageSize(Number(event.target.value))}
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
                                <th>{t("Description")}</th>
                                <th>{t("CreatedAt")}</th>
                                <th>{t("UpdatedAt")}</th>
                                <th className="text-center!">{t("Actions")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {categories.data?.items.map((category) => (
                                <tr key={category.id}>
                                    <td className="text-right">{category.id}</td>
                                    <td>{category.description}</td>
                                    <td>{new Date(category.createdAt).toLocaleString()}</td>
                                    <td>
                                        {category.updatedAt
                                            ? new Date(category.updatedAt).toLocaleString()
                                            : "—"}
                                    </td>
                                    <td>
                                        <div className="flex justify-center gap-1">
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                onClick={() =>
                                                    setDialog({ id: category.id, mode: "view" })
                                                }
                                            >
                                                <Eye className="h-3 w-3" />
                                                {t("View")}
                                            </Button>
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                onClick={() =>
                                                    setDialog({ id: category.id, mode: "edit" })
                                                }
                                            >
                                                <Pencil className="h-3 w-3" />
                                                {t("Edit")}
                                            </Button>
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                className="text-danger"
                                                onClick={() => setDeleting(category.id)}
                                            >
                                                <Trash2 className="h-3 w-3" />
                                                {t("Delete")}
                                            </Button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                            {!categories.data?.items.length && (
                                <tr>
                                    <td colSpan={5} className="p-8 text-center text-muted">
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
            {dialog && (
                <CategoryDialog
                    id={dialog.id}
                    mode={dialog.mode}
                    open
                    onOpenChange={(open) => !open && setDialog(null)}
                />
            )}
            <ConfirmDialog
                open={deleting !== null}
                title={t("ConfirmDelete")}
                onOpenChange={(open) => !open && setDeleting(null)}
                onConfirm={() => deleting && remove.mutate(deleting)}
            />
        </div>
    );
}
