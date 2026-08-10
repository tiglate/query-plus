namespace QueryPlus.Api.Security;

/// <summary>
/// Realm role names (see docker/keycloak/realm-export.json) and the [Authorize(Roles=...)]
/// combinations controllers use. ROLE_ADMIN is appended to every combination so it always
/// implies every other permission.
/// </summary>
public static class AppRoles
{
    public const string Admin = "ROLE_ADMIN";
    public const string CategoryRead = "ROLE_CATEGORY_READ";
    public const string CategoryWrite = "ROLE_CATEGORY_WRITE";
    public const string ProcedureRead = "ROLE_PROCEDURE_READ";
    public const string ProcedureWrite = "ROLE_PROCEDURE_WRITE";
    public const string QueryExec = "ROLE_QUERY_EXEC";
    public const string JobRead = "ROLE_JOB_READ";
    public const string JobWrite = "ROLE_JOB_WRITE";
    public const string JobApprove = "ROLE_JOB_APPROVE";

    public const string CanReadCategories = CategoryRead + "," + CategoryWrite + "," + Admin;
    public const string CanWriteCategories = CategoryWrite + "," + Admin;

    public const string CanReadProcedures = ProcedureRead + "," + ProcedureWrite + "," + Admin;
    public const string CanWriteProcedures = ProcedureWrite + "," + Admin;

    public const string CanReadJobs = JobRead + "," + JobWrite + "," + JobApprove + "," + Admin;
    public const string CanWriteJobs = JobWrite + "," + Admin;
    public const string CanApproveJobs = JobApprove + "," + Admin;

    /// <summary>Endpoints procedure admins need (category dropdown) that aren't category-specific.</summary>
    public const string CanReadCategoryLookup = CategoryRead + "," + CategoryWrite + "," + ProcedureRead + "," +
                                                 ProcedureWrite + "," + Admin;

    /// <summary>Endpoints the interactive execute flow needs, in addition to catalog admin roles.</summary>
    public const string CanReadOrExecuteProcedures = ProcedureRead + "," + ProcedureWrite + "," + QueryExec + "," +
                                                       Admin;

    public const string CanExecute = QueryExec + "," + Admin;
}
