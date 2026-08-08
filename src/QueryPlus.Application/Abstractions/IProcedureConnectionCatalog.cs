namespace QueryPlus.Application.Abstractions;

/// <summary>
/// Lists the names configured under the app's ConnectionStrings section, i.e. the SQL Server
/// targets a catalogued procedure may run against.
/// </summary>
public interface IProcedureConnectionCatalog
{
    IReadOnlyCollection<string> GetConnectionNames();
}
