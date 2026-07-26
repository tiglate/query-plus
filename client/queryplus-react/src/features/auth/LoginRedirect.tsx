import { LoaderCircle } from "lucide-react";
import { useEffect } from "react";

export function LoginRedirect() {
    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const returnUrl = params.get("returnUrl") ?? "/";
        window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
    }, []);
    return (
        <div className="grid min-h-dvh place-items-center">
            <LoaderCircle className="h-6 w-6 animate-spin text-cyan-500" />
        </div>
    );
}
