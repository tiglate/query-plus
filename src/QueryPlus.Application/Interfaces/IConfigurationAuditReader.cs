using QueryPlus.Application.DTOs.Common;

namespace QueryPlus.Application.Interfaces;

public interface IConfigurationAuditReader
{
    Task<AuditDetailsDto> GetCategoryAuditDetailsAsync(
        int categoryId,
        CancellationToken cancellationToken = default);

    Task<AuditDetailsDto> GetProcedureAuditDetailsAsync(
        int procedureId,
        CancellationToken cancellationToken = default);
}
