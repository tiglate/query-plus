using QueryPlus.Application.DTOs.Procedures;

namespace QueryPlus.Application.Interfaces;

/// <summary>
/// Loads parameter/column metadata from SQL Server system views for Sync Metadata.
/// </summary>
public interface IProcedureMetadataSyncService
{
    Task<ProcedureMetadataSnapshot> FetchAsync(
        string databaseName,
        string procedureName,
        CancellationToken cancellationToken = default);
}

public sealed class ProcedureMetadataSnapshot
{
    public IReadOnlyList<SaveProcedureParameterDto> Parameters { get; init; } = [];
    public IReadOnlyList<SaveProcedureColumnDto> Columns { get; init; } = [];
}
