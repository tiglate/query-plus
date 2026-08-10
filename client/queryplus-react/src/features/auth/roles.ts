/**
 * Mirrors QueryPlus.Api.Security.AppRoles (backend) - keep in sync. ROLE_ADMIN is included
 * in every group below because it implies every other permission server-side.
 */
export const ROLE_ADMIN = "ROLE_ADMIN";

export const CATEGORY_ROLES = ["ROLE_CATEGORY_READ", "ROLE_CATEGORY_WRITE", ROLE_ADMIN];
export const PROCEDURE_ROLES = ["ROLE_PROCEDURE_READ", "ROLE_PROCEDURE_WRITE", ROLE_ADMIN];
export const EXECUTION_LOG_ROLES = [ROLE_ADMIN];
export const JOB_ROLES = ["ROLE_JOB_READ", "ROLE_JOB_WRITE", "ROLE_JOB_APPROVE", ROLE_ADMIN];
export const JOB_WRITE_ROLES = ["ROLE_JOB_WRITE", ROLE_ADMIN];
export const JOB_APPROVE_ROLES = ["ROLE_JOB_APPROVE", ROLE_ADMIN];
export const ADMIN_AREA_ROLES = [
    ...CATEGORY_ROLES,
    ...PROCEDURE_ROLES,
    ...EXECUTION_LOG_ROLES,
    ...JOB_ROLES,
];

export function hasAnyRole(
    userRoles: readonly string[] | undefined,
    required: readonly string[],
): boolean {
    if (!userRoles || userRoles.length === 0) return false;
    return required.some((role) => userRoles.includes(role));
}
