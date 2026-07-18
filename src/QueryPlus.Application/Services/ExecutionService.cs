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

public sealed class ExecutionService : IExecutionService
{
    private readonly IProcedureRepository _procedures;
    private readonly IExecutionRepository _executions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoredProcedureExecutor _executor;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMapper _mapper;
    private readonly IValidator<ExecuteProcedureRequest> _requestValidator;
    private readonly ILogger<ExecutionService> _logger;

    public ExecutionService(
        IProcedureRepository procedures,
        IExecutionRepository executions,
        IUnitOfWork unitOfWork,
        IStoredProcedureExecutor executor,
        ICurrentUserContext currentUser,
        IMapper mapper,
        IValidator<ExecuteProcedureRequest> requestValidator,
        ILogger<ExecutionService> logger)
    {
        _procedures = procedures;
        _executions = executions;
        _unitOfWork = unitOfWork;
        _executor = executor;
        _currentUser = currentUser;
        _mapper = mapper;
        _requestValidator = requestValidator;
        _logger = logger;
    }

    public async Task<ExecutionResultDto> ExecuteAsync(
        ExecuteProcedureRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(_requestValidator, request, cancellationToken);

        if (!_currentUser.IsAuthenticated)
        {
            throw new ForbiddenOperationException("Authentication is required to execute procedures.");
        }

        var procedure = await _procedures.GetEnabledByIdWithDetailsAsync(request.ProcedureId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Procedure), request.ProcedureId);

        EnsureUserMayExecute(procedure);

        // Bind & type-coerce form values using metadata (never pass raw strings blindly).
        var boundParameters = ParameterValueBinder.Bind(procedure.Parameters, request.ParameterValues);

        var log = new ExecutionLog
        {
            IdProcedure = procedure.IdProcedure,
            Username = _currentUser.Username,
            IpAddress = _currentUser.IpAddress,
            ExecutionStart = DateTime.UtcNow,
            ParameterValues = JsonHelpers.Serialize(
                boundParameters.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString())),
            Success = false
        };

        await _executions.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Secure path: only identifiers from the catalog + bound parameters (no free SQL).
            var data = await _executor.ExecuteAsync(
                procedure.DatabaseName,
                procedure.ProcedureName,
                boundParameters,
                cancellationToken);

            log.Success = true;
            log.RowCount = data.Rows.Count;
            log.ExecutionEnd = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var columns = BuildGridColumns(procedure, data);

            return new ExecutionResultDto
            {
                Success = true,
                ExecutionLogId = log.IdExecutionLog,
                ProcedureId = procedure.IdProcedure,
                ProcedureCaption = procedure.Caption,
                RowCount = data.Rows.Count,
                Data = data,
                Columns = columns
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Stored procedure execution failed. ProcedureId={ProcedureId}, User={User}",
                procedure.IdProcedure,
                _currentUser.Username);

            log.Success = false;
            log.ErrorMessage = Truncate(ex.Message, 4000);
            log.ExecutionEnd = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ExecutionResultDto
            {
                Success = false,
                ErrorMessage = "The stored procedure failed to execute. See execution log for details.",
                ExecutionLogId = log.IdExecutionLog,
                ProcedureId = procedure.IdProcedure,
                ProcedureCaption = procedure.Caption,
                RowCount = 0,
                Data = null,
                Columns = _mapper.Map<IReadOnlyList<GridColumnDto>>(
                    procedure.Columns.Where(c => c.Visible).OrderBy(c => c.Caption).ToList())
            };
        }
    }

    public async Task<IReadOnlyList<ExecutionLogDto>> GetRecentByProcedureAsync(
        int procedureId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var logs = await _executions.GetByProcedureAsync(procedureId, take, cancellationToken);
        return _mapper.Map<IReadOnlyList<ExecutionLogDto>>(logs);
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

        if (!required.Any(r => _currentUser.IsInRole(r)))
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
