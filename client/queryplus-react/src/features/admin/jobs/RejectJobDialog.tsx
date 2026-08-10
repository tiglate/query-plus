import * as Dialog from "@radix-ui/react-dialog";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/fields";

interface RejectJobDialogProps {
    open: boolean;
    jobName?: string;
    onOpenChange: (open: boolean) => void;
    onConfirm: (reason: string) => void;
}

export function RejectJobDialog({
    open,
    jobName,
    onOpenChange,
    onConfirm,
}: Readonly<RejectJobDialogProps>) {
    const { t } = useTranslation();
    const [reason, setReason] = useState("");

    useEffect(() => {
        if (open) setReason("");
    }, [open]);

    const trimmed = reason.trim();

    return (
        <Dialog.Root open={open} onOpenChange={onOpenChange}>
            <Dialog.Portal>
                <Dialog.Overlay className="fixed inset-0 z-50 bg-black/45" />
                <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-[min(28rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white p-5 shadow-xl dark:bg-navy-800">
                    <div className="flex items-center justify-between">
                        <Dialog.Title className="text-card-title font-semibold">
                            {t("Jobs_ConfirmReject")}
                        </Dialog.Title>
                        <Dialog.Close asChild>
                            <Button type="button" variant="ghost" size="icon">
                                <X className="h-4 w-4" />
                            </Button>
                        </Dialog.Close>
                    </div>
                    {jobName && (
                        <Dialog.Description className="mt-2 text-body text-slate-600 dark:text-slate-300">
                            {jobName}
                        </Dialog.Description>
                    )}
                    <label className="mt-4 block text-small-label font-medium text-slate-700 dark:text-slate-200">
                        <span className="after:ml-1 after:text-danger after:content-['*']">
                            {t("Jobs_RejectReason")}
                        </span>
                        <Textarea
                            className="mt-1"
                            rows={3}
                            value={reason}
                            onChange={(event) => setReason(event.target.value)}
                        />
                    </label>
                    <div className="mt-5 flex justify-end gap-2">
                        <Dialog.Close asChild>
                            <Button type="button" variant="secondary">
                                {t("Cancel")}
                            </Button>
                        </Dialog.Close>
                        <Button
                            type="button"
                            variant="danger"
                            disabled={!trimmed}
                            onClick={() => {
                                onConfirm(trimmed);
                                onOpenChange(false);
                            }}
                        >
                            {t("Jobs_Reject")}
                        </Button>
                    </div>
                </Dialog.Content>
            </Dialog.Portal>
        </Dialog.Root>
    );
}
