import { useCallback, useState } from "react";
import { Maximize2, Minimize2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";

export const RESULTS_MAX_STORAGE_KEY = "qp-home-results-maximized";

function initialMaximized(): boolean {
    try {
        return sessionStorage.getItem(RESULTS_MAX_STORAGE_KEY) === "1";
    } catch {
        return false;
    }
}

export function useResultsMaximize() {
    const [maximized, setMaximized] = useState(initialMaximized);
    const toggle = useCallback(() => {
        setMaximized((current) => {
            const next = !current;
            try {
                sessionStorage.setItem(RESULTS_MAX_STORAGE_KEY, next ? "1" : "0");
            } catch {
                return next;
            }
            return next;
        });
    }, []);
    return { maximized, toggle };
}

export function MaximizeButton({
    maximized,
    onToggle,
}: {
    maximized: boolean;
    onToggle: () => void;
}) {
    const { t } = useTranslation();
    const label = t(maximized ? "Home_RestoreGrid" : "Home_MaximizeGrid");
    return (
        <Button
            id="btn-toggle-results-max"
            type="button"
            variant="ghost"
            size="sm"
            aria-pressed={maximized}
            title={label}
            onClick={onToggle}
        >
            {maximized ? <Minimize2 className="h-4 w-4" /> : <Maximize2 className="h-4 w-4" />}
            {label}
        </Button>
    );
}
