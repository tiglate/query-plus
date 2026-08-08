using FluentValidation;
using Microsoft.Extensions.Logging;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class ExecutionService(
    IProcedureRepository procedures,
    IExecutionRepository executions,
    IUnitOfWork unitOfWork,
    IStoredProcedureExecutor executor,
    ICurrentUserContext currentUser,
    IValidator<ExecuteProcedureRequest> requestValidator,
    IExecutionParameterResolver parameterResolver,
    IGridColumnBuilder columnBuilder,
    ILogger<ExecutionService> logger)
    : IExecutionService
{
    public async Task<ExecutionResultDto> ExecuteAsync(
        ExecuteProcedureRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(requestValidator, request, cancellationToken);

        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenOperationException("Authentication is required to execute procedures.");
        }

        var procedure = await procedures.GetEnabledByIdWithDetailsAsync(request.ProcedureId, cancellationToken)
                        ?? throw new EntityNotFoundException(nameof(Procedure), request.ProcedureId);

        EnsureUserMayExecute(procedure);

        var resolved = parameterResolver.Resolve(
            procedure,
            request.ParameterValues,
            request.PageNumber,
            request.PageSize);

        var sensitiveNames = procedure.Parameters
            .Where(p => p.IsSensitive)
            .Select(p => SqlIdentifier.NormalizeParameterName(p.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var loggedParameters = resolved.BoundUserParameters
            .ToDictionary(
                kv => kv.Key,
                kv => sensitiveNames.Contains(kv.Key) ? "***" : kv.Value?.ToString());

        var log = new ExecutionLog
        {
            IdProcedure = procedure.IdProcedure,
            Username = currentUser.Username,
            IpAddress = currentUser.IpAddress,
            ExecutionStart = DateTime.UtcNow,
            ParameterValues = JsonHelpers.Serialize(loggedParameters),
            Success = false
        };

        await executions.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var executed = await executor.ExecuteAsync(
                procedure.DatabaseName,
                procedure.ProcedureName,
                resolved.ExecParameters,
                resolved.OutputParameterNames,
                cancellationToken);

            var data = executed.Data;
            log.Success = true;
            // Audit: prefer total for paginated SPs; otherwise page/full row count.
            log.RowCount = procedure.SupportsPagination
                ? (int)Math.Min(executed.TotalRecords ?? data.Rows.Count, int.MaxValue)
                : data.Rows.Count;
            log.ExecutionEnd = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var columns = columnBuilder.BuildGridColumns(procedure, data);

            return new ExecutionResultDto
            {
                Success = true,
                ExecutionLogId = log.IdExecutionLog,
                ProcedureId = procedure.IdProcedure,
                ProcedureCaption = procedure.Caption,
                RowCount = data.Rows.Count,
                SupportsPagination = procedure.SupportsPagination,
                PageNumber = resolved.PageNumber,
                PageSize = resolved.PageSize,
                TotalRecords = executed.TotalRecords,
                Data = data,
                Columns = columns
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Stored procedure execution failed. ProcedureId={ProcedureId}, User={User}",
                procedure.IdProcedure,
                currentUser.Username);

            log.Success = false;
            log.ErrorMessage = Truncate(ex.Message, 4000);
            log.ExecutionEnd = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ExecutionResultDto
            {
                Success = false,
                ErrorMessage = "The stored procedure failed to execute. See execution log for details.",
                ExecutionLogId = log.IdExecutionLog,
                ProcedureId = procedure.IdProcedure,
                ProcedureCaption = procedure.Caption,
                RowCount = 0,
                SupportsPagination = procedure.SupportsPagination,
                PageNumber = resolved.PageNumber,
                PageSize = resolved.PageSize,
                Data = null,
                Columns = ProcedureColumnMapper.ToGridColumnDtos(
                    procedure.Columns.Where(c => c.Visible).OrderBy(c => c.Caption).ToList())
            };
        }
    }

    public async Task<IReadOnlyList<ExecutionLogDto>> GetRecentByProcedureAsync(
        int procedureId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var logs = await executions.GetByProcedureAsync(procedureId, take, cancellationToken);
        return ExecutionLogMapper.ToDtos(logs);
    }

    public async Task<PagedResult<ExecutionLogListItemDto>> SearchAsync(
        ExecutionLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // Dates come in as local calendar days (browser <input type=date>); the
        // repository compares against ExecutionStart, which is stored in UTC.
        var criteria = new ExecutionLogSearchCriteria
        {
            Username = filter.Username,
            ProcedureId = filter.ProcedureId,
            Success = filter.Success,
            StartFrom = filter.StartFrom is { } from
                ? DateTime.SpecifyKind(from.Date, DateTimeKind.Local).ToUniversalTime()
                : null,
            StartTo = filter.StartTo is { } to
                ? DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime()
                : null
        };

        var (page, pageSize) = PagedResult<ExecutionLogListItemDto>.Normalize(filter.Page, filter.PageSize);

        var (items, totalCount) = await executions.SearchAsync(criteria, page, pageSize, cancellationToken);

        // If the requested page is past the end, clamp and re-fetch once.
        if (totalCount > 0 && (page - 1) * pageSize >= totalCount)
        {
            (page, pageSize) = PagedResult<ExecutionLogListItemDto>.Normalize(page, pageSize, totalCount);
            (items, totalCount) = await executions.SearchAsync(criteria, page, pageSize, cancellationToken);
        }

        return new PagedResult<ExecutionLogListItemDto>
        {
            Items = ExecutionLogMapper.ToListItemDtos(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private void EnsureUserMayExecute(Procedure procedure)
    {
        if (!procedure.Enabled)
        {
            throw new ForbiddenOperationException("This procedure is disabled.");
        }

        if (!procedure.IsAccessibleTo(currentUser.Roles))
        {
            throw new ForbiddenOperationException(
                $"You do not have the required entitlement '{procedure.RoleEntitlement}' to execute this procedure.");
        }
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
