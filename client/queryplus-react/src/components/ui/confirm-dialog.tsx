import * as AlertDialog from "@radix-ui/react-alert-dialog";
import { useTranslation } from "react-i18next";
import { Button } from "./button";

interface ConfirmDialogProps {
    open: boolean;
    title: string;
    description?: string;
    onOpenChange: (open: boolean) => void;
    onConfirm: () => void;
}

export function ConfirmDialog({
    open,
    title,
    description,
    onOpenChange,
    onConfirm,
}: Readonly<ConfirmDialogProps>) {
    const { t } = useTranslation();
    return (
        <AlertDialog.Root open={open} onOpenChange={onOpenChange}>
            <AlertDialog.Portal>
                <AlertDialog.Overlay className="fixed inset-0 z-50 bg-black/45" />
                <AlertDialog.Content className="fixed left-1/2 top-1/2 z-50 w-[min(28rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white p-5 shadow-xl dark:bg-navy-800">
                    <AlertDialog.Title className="text-card-title font-semibold">
                        {title}
                    </AlertDialog.Title>
                    {description && (
                        <AlertDialog.Description className="mt-2 text-body text-slate-600 dark:text-slate-300">
                            {description}
                        </AlertDialog.Description>
                    )}
                    <div className="mt-5 flex justify-end gap-2">
                        <AlertDialog.Cancel asChild>
                            <Button variant="secondary">{t("Cancel")}</Button>
                        </AlertDialog.Cancel>
                        <AlertDialog.Action asChild>
                            <Button variant="danger" onClick={onConfirm}>
                                {t("Yes")}
                            </Button>
                        </AlertDialog.Action>
                    </div>
                </AlertDialog.Content>
            </AlertDialog.Portal>
        </AlertDialog.Root>
    );
}
