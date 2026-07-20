using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Interfaces;
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
    IMapper mapper,
    IValidator<ExecuteProcedureRequest> requestValidator,
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

        // Bind only non-reserved catalog parameters (pagination args are injected by the app).
        var userParameterDefs = procedure.Parameters
            .Where(p => !ProcedurePagination.IsReservedParameterName(p.Name))
            .ToList();
        var boundParameters = ParameterValueBinder.Bind(userParameterDefs, request.ParameterValues);

        var log = new ExecutionLog
        {
            IdProcedure = procedure.IdProcedure,
            Username = currentUser.Username,
            IpAddress = currentUser.IpAddress,
            ExecutionStart = DateTime.UtcNow,
            ParameterValues = JsonHelpers.Serialize(
                boundParameters.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString())),
            Success = false
        };

        await executions.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var pageNumber = ProcedurePagination.DefaultPageNumber;
        var pageSize = ProcedurePagination.DefaultPageSize;

        try
        {
            IReadOnlyDictionary<string, object?> execParameters = boundParameters;
            IReadOnlyCollection<string>? outputs = null;

            if (procedure.SupportsPagination)
            {
                pageNumber = ProcedurePagination.ClampPageNumber(request.PageNumber);
                pageSize = ProcedurePagination.ClampUiPageSize(request.PageSize);
                execParameters = ProcedurePagination.WithPagingInputs(
                    boundParameters,
                    pageNumber,
                    pageSize);
                outputs = [ProcedurePagination.TotalRecordsName];
            }

            var executed = await executor.ExecuteAsync(
                procedure.DatabaseName,
                procedure.ProcedureName,
                execParameters,
                outputs,
                cancellationToken);

            var data = executed.Data;
            log.Success = true;
            // Audit: prefer total for paginated SPs; otherwise page/full row count.
            log.RowCount = procedure.SupportsPagination
                ? (int)Math.Min(executed.TotalRecords ?? data.Rows.Count, int.MaxValue)
                : data.Rows.Count;
            log.ExecutionEnd = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var columns = BuildGridColumns(procedure, data);

            return new ExecutionResultDto
            {
                Success = true,
                ExecutionLogId = log.IdExecutionLog,
                ProcedureId = procedure.IdProcedure,
                ProcedureCaption = procedure.Caption,
                RowCount = data.Rows.Count,
                SupportsPagination = procedure.SupportsPagination,
                PageNumber = pageNumber,
                PageSize = pageSize,
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
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = null,
                Columns = mapper.Map<IReadOnlyList<GridColumnDto>>(
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
        return mapper.Map<IReadOnlyList<ExecutionLogDto>>(logs);
    }

    private void EnsureUserMayExecute(Procedure procedure)
    {
        if (!procedure.Enabled)
        {
            throw new ForbiddenOperationException("This procedure is disabled.");
        }

        var entitlement = procedure.RoleEntitlement.Trim();
        if (string.IsNullOrEmpty(entitlement))
        {
            return;
        }

        // Role entitlement may be a single role or comma-separated list.
        var required = entitlement
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (required.Length == 0)
        {
            return;
        }

        if (!required.Any(r => currentUser.IsInRole(r)))
        {
            throw new ForbiddenOperationException(
                $"You do not have the required entitlement '{entitlement}' to execute this procedure.");
        }
    }

    private static IReadOnlyList<GridColumnDto> BuildGridColumns(Procedure procedure, System.Data.DataTable data)
    {
        var configured = procedure.Columns
            .Where(c => c.Visible)
            .ToDictionary(c => c.TechnicalName, c => c, StringComparer.OrdinalIgnoreCase);

        var columns = new List<GridColumnDto>();
        foreach (System.Data.DataColumn col in data.Columns)
        {
            if (configured.TryGetValue(col.ColumnName, out var meta))
            {
                columns.Add(new GridColumnDto
                {
                    TechnicalName = meta.TechnicalName,
                    Caption = meta.Caption,
                    Alignment = meta.Alignment,
                    FormatMask = meta.FormatMask,
                    Visible = meta.Visible
                });
            }
            else
            {
                // Fallback: show result columns not yet configured in metadata.
                columns.Add(new GridColumnDto
                {
                    TechnicalName = col.ColumnName,
                    Caption = col.ColumnName,
                    Alignment = Domain.Enums.ColumnAlignment.Left,
                    Visible = true
                });
            }
        }

        return columns;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
