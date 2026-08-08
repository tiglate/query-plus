import {
    ADMIN_AREA_ROLES,
    CATEGORY_ROLES,
    EXECUTION_LOG_ROLES,
    PROCEDURE_ROLES,
    hasAnyRole,
} from "./roles";

test("hasAnyRole is true when the user holds one of the required roles", () => {
    expect(hasAnyRole(["ROLE_CATEGORY_READ"], CATEGORY_ROLES)).toBe(true);
});

test("hasAnyRole is true for ROLE_ADMIN against every admin-area role group", () => {
    expect(hasAnyRole(["ROLE_ADMIN"], CATEGORY_ROLES)).toBe(true);
    expect(hasAnyRole(["ROLE_ADMIN"], PROCEDURE_ROLES)).toBe(true);
    expect(hasAnyRole(["ROLE_ADMIN"], EXECUTION_LOG_ROLES)).toBe(true);
    expect(hasAnyRole(["ROLE_ADMIN"], ADMIN_AREA_ROLES)).toBe(true);
});

test("hasAnyRole is false when the user holds none of the required roles", () => {
    expect(hasAnyRole(["ROLE_QUERY_EXEC"], CATEGORY_ROLES)).toBe(false);
    expect(hasAnyRole(["ROLE_QUERY_EXEC"], PROCEDURE_ROLES)).toBe(false);
    expect(hasAnyRole(["ROLE_QUERY_EXEC"], EXECUTION_LOG_ROLES)).toBe(false);
});

test("hasAnyRole is false for undefined or empty roles", () => {
    expect(hasAnyRole(undefined, ADMIN_AREA_ROLES)).toBe(false);
    expect(hasAnyRole([], ADMIN_AREA_ROLES)).toBe(false);
});
