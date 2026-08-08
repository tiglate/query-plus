import { useQuery } from "@tanstack/react-query";
import { authQuery } from "@/api/queries";

export function useUser() {
    return useQuery(authQuery);
}
