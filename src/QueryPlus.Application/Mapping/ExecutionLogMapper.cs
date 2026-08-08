using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

/// <summary>
/// Entity → DTO projections for <see cref="ExecutionLog"/>. Two target
/// shapes: the full <see cref="ExecutionLogDto"/> and the list-optimized
/// <see cref="ExecutionLogListItemDto"/> that surfaces the procedure caption.
/// </summary>
public static class ExecutionLogMapper
{
    public static ExecutionLogDto ToDto(ExecutionLog entity) => new()
    {
        Id = entity.IdExecutionLog,
        ProcedureId = entity.IdProcedure,
        Username = entity.Username,
        IpAddress = entity.IpAddress,
        ExecutionStart = entity.ExecutionStart,
        ExecutionEnd = entity.ExecutionEnd,
        Success = entity.Success,
        ErrorMessage = entity.ErrorMessage,
        ParameterValuesJson = entity.ParameterValues,
        RowCount = entity.RowCount,
    };

    public static IReadOnlyList<ExecutionLogDto> ToDtos(IEnumerable<ExecutionLog> entities) =>
        entities.Select(ToDto).ToArray();

    public static ExecutionLogListItemDto ToListItemDto(ExecutionLog entity) => new()
    {
        Id = entity.IdExecutionLog,
        ProcedureId = entity.IdProcedure,
        ProcedureCaption = entity.Procedure is null ? string.Empty : entity.Procedure.Caption,
        Username = entity.Username,
        IpAddress = entity.IpAddress,
        ExecutionStart = entity.ExecutionStart,
        ExecutionEnd = entity.ExecutionEnd,
        Success = entity.Success,
        ErrorMessage = entity.ErrorMessage,
        RowCount = entity.RowCount,
    };

    public static IReadOnlyList<ExecutionLogListItemDto> ToListItemDtos(IEnumerable<ExecutionLog> entities) =>
        entities.Select(ToListItemDto).ToArray();
}
