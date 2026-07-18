using QueryPlus.Domain.Entities;

namespace QueryPlus.Domain.Interfaces;

public interface IProcedureRepository
{
    Task<Procedure?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads procedure with parameters, columns and category (for admin edit/view).
    /// </summary>
    Task<Procedure?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads enabled procedure with parameters/columns for execution.
    /// </summary>
    Task<Procedure?> GetEnabledByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Procedure>> SearchAsync(
        ProcedureSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procedures the user may execute (enabled + matching role entitlement).
    /// </summary>
    Task<IReadOnlyList<Procedure>> GetAccessibleForExecutionAsync(
        IReadOnlyCollection<string> userRoles,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByCaptionAsync(
        string caption,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByDatabaseAndNameAsync(
        string databaseName,
        string procedureName,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Procedure procedure, CancellationToken cancellationToken = default);
    void Update(Procedure procedure);
    void Remove(Procedure procedure);

    void RemoveParameter(ProcedureParameter parameter);
    void RemoveColumn(ProcedureColumn column);
}

public sealed class ProcedureSearchCriteria
{
    public int? CategoryId { get; init; }
    public string? Caption { get; init; }
    public string? RoleEntitlement { get; init; }
    public bool? Enabled { get; init; }
    public string? DatabaseName { get; init; }
    public string? ProcedureName { get; init; }
}
