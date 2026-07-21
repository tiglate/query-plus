using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;

namespace QueryPlus.Application.Interfaces;

public interface IProcedureService
{
    Task<PagedResult<ProcedureListItemDto>> SearchAsync(
        ProcedureFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<ProcedureDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// All procedures ordered by caption (admin dropdowns / lookups).
    /// </summary>
    Task<IReadOnlyList<ProcedureLookupDto>> ListAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procedures the current user may execute (Home combo).
    /// </summary>
    Task<IReadOnlyList<ProcedureLookupDto>> GetAccessibleForCurrentUserAsync(
        CancellationToken cancellationToken = default);

    Task<ProcedureDetailDto> CreateAsync(
        SaveProcedureDto dto,
        CancellationToken cancellationToken = default);

    Task<ProcedureDetailDto> UpdateAsync(
        SaveProcedureDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
