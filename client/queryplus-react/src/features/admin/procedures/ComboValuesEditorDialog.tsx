import * as Dialog from "@radix-ui/react-dialog";
import { Plus, Trash2, X } from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

interface ComboValuesEditorDialogProps {
    open: boolean;
    initialJson: string | null | undefined;
    readOnly?: boolean;
    onOpenChange: (open: boolean) => void;
    onSave: (json: string) => void;
}

function parseValues(raw: string | null | undefined): string[] {
    try {
        const parsed: unknown = JSON.parse(raw || "[]");
        if (Array.isArray(parsed) && parsed.every((item) => typeof item === "string")) {
            return parsed;
        }
    } catch {
        // invalid JSON: start the editor from an empty list rather than blocking it
    }
    return [];
}

export function ComboValuesEditorDialog({
    open,
    initialJson,
    readOnly,
    onOpenChange,
    onSave,
}: Readonly<ComboValuesEditorDialogProps>) {
    const { t } = useTranslation();
    const [values, setValues] = useState<string[]>([]);

    useEffect(() => {
        if (open) setValues(parseValues(initialJson));
    }, [open, initialJson]);

    return (
        <Dialog.Root open={open} onOpenChange={onOpenChange}>
            <Dialog.Portal>
                <Dialog.Overlay className="fixed inset-0 z-50 bg-black/45" />
                <Dialog.Content className="fixed left-1/2 top-1/2 z-50 flex max-h-[80vh] w-[min(28rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 flex-col rounded-lg bg-white p-5 shadow-xl dark:bg-navy-800">
                    <div className="flex items-center justify-between">
                        <Dialog.Title className="text-card-title font-semibold">
                            {t("Procedures_ComboValues_Title")}
                        </Dialog.Title>
                        <Dialog.Close asChild>
                            <Button type="button" variant="ghost" size="icon">
                                <X className="h-4 w-4" />
                            </Button>
                        </Dialog.Close>
                    </div>
                    <div className="mt-4 flex-1 space-y-2 overflow-y-auto">
                        {values.length === 0 && (
                            <p className="text-body text-muted">
                                {t("Procedures_ComboValues_Empty")}
                            </p>
                        )}
                        {values.map((value, index) => (
                            <div key={index} className="flex items-center gap-2">
                                <Input
                                    readOnly={readOnly}
                                    placeholder={t("Procedures_ComboValues_Placeholder")}
                                    value={value}
                                    onChange={(event) =>
                                        setValues((current) =>
                                            current.map((item, i) =>
                                                i === index ? event.target.value : item,
                                            ),
                                        )
                                    }
                                />
                                {!readOnly && (
                                    <Button
                                        type="button"
                                        variant="ghost"
                                        size="icon"
                                        className="shrink-0 text-danger"
                                        aria-label={t("Procedures_ComboValues_Remove")}
                                        onClick={() =>
                                            setValues((current) =>
                                                current.filter((_, i) => i !== index),
                                            )
                                        }
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </Button>
                                )}
                            </div>
                        ))}
                    </div>
                    {!readOnly && (
                        <Button
                            type="button"
                            variant="secondary"
                            className="mt-3 self-start"
                            onClick={() => setValues((current) => [...current, ""])}
                        >
                            <Plus className="h-4 w-4" />
                            {t("Procedures_ComboValues_AddValue")}
                        </Button>
                    )}
                    <div className="mt-5 flex justify-end gap-2">
                        <Dialog.Close asChild>
                            <Button type="button" variant="secondary">
                                {t(readOnly ? "Back" : "Cancel")}
                            </Button>
                        </Dialog.Close>
                        {!readOnly && (
                            <Button
                                type="button"
                                onClick={() => {
                                    const cleaned = values
                                        .map((value) => value.trim())
                                        .filter((value) => value !== "");
                                    onSave(JSON.stringify(cleaned));
                                    onOpenChange(false);
                                }}
                            >
                                {t("Save")}
                            </Button>
                        )}
                    </div>
                </Dialog.Content>
            </Dialog.Portal>
        </Dialog.Root>
    );
}
