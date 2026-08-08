using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QueryPlus.Api.DependencyInjection;
using QueryPlus.Api.Security;
using QueryPlus.Api.Services;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Services;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/execute")]
[Authorize(Roles = AppRoles.CanExecute)]
[EnableRateLimiting(RateLimitingServiceCollectionExtensions.ExecutePolicy)]
public sealed class ExecuteController(
    IProcedureRepository repository,
    IExecutionService execution,
    ExportEligibilityService eligibility,
    ICurrentUserContext user) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Execute(ExecuteProcedureRequest request, CancellationToken cancellationToken)
    {
        var values = Normalize(request.ParameterValues, out var reserved);
        if (reserved.Count > 0)
            return Problem(title: "Reserved pagination parameters are not accepted",
                detail: string.Join(", ", reserved), statusCode: 400);
        if (request.ProcedureId <= 0)
        {
            eligibility.Clear(user.Username);
            return Problem(title: "Procedure is required", statusCode: 400);
        }

        var procedure = await repository.GetEnabledByIdWithDetailsAsync(request.ProcedureId, cancellationToken);
        if (procedure is null)
        {
            eligibility.Clear(user.Username);
            return Problem(title: "Procedure not found or disabled", statusCode: 404);
        }

        var missing = ParameterValueBinder.GetMissingRequiredCaptions(procedure.Parameters, values);
        if (missing.Count > 0)
        {
            eligibility.Clear(user.Username);
            return Problem(title: "Required parameters are missing", detail: string.Join(", ", missing),
                statusCode: 400);
        }

        try
        {
            var result = await execution.ExecuteAsync(
                new()
                {
                    ProcedureId = request.ProcedureId, ParameterValues = values,
                    PageNumber = ProcedurePagination.ClampPageNumber(request.PageNumber),
                    PageSize = ProcedurePagination.ClampUiPageSize(request.PageSize)
                }, cancellationToken);
            if (result.Success)
            {
                var rows = result.SupportsPagination
                    ? (int)Math.Min(result.TotalRecords ?? result.RowCount, int.MaxValue)
                    : result.RowCount;
                if (rows > 0) eligibility.MarkEligible(user.Username, request.ProcedureId, values, rows);
                else eligibility.Clear(user.Username);
            }
            else eligibility.Clear(user.Username);

            return Ok(ToResponse(result, procedure));
        }
        catch (AppValidationException ex)
        {
            eligibility.Clear(user.Username);
            return BadRequest(new ValidationProblemDetails(ex.Errors.ToDictionary(x => x.Key, x => x.Value))
                { Title = "Validation failed", Status = 400 });
        }
        catch (EntityNotFoundException ex)
        {
            eligibility.Clear(user.Username);
            return Problem(title: "Procedure not found", detail: ex.Message, statusCode: 404);
        }
        catch (ForbiddenOperationException ex)
        {
            eligibility.Clear(user.Username);
            return Problem(title: "Procedure access denied", detail: ex.Message, statusCode: 403);
        }
    }

    private static Dictionary<string, string?> Normalize(IDictionary<string, string?>? source,
        out List<string> reserved)
    {
        reserved = [];
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source ?? new Dictionary<string, string?>())
        {
            var name = pair.Key.Trim().TrimStart('@');
            if (ProcedurePagination.IsReservedParameterName(name))
            {
                reserved.Add(pair.Key);
                continue;
            }

            if (name.Length > 0) result[name] = pair.Value?.Trim();
        }

        return result;
    }

    private static object ToResponse(ExecutionResultDto result, Procedure procedure)
    {
        if (result.Data is null)
        {
            return new
            {
                result.Success,
                result.ErrorMessage,
                result.ExecutionLogId,
                result.ProcedureId,
                result.ProcedureCaption,
                result.RowCount,
                result.SupportsPagination,
                result.PageNumber,
                result.PageSize,
                result.TotalRecords,
                columns = Array.Empty<GridColumnDto>(),
                rows = Array.Empty<object?[]>()
            };
        }

        var visibility = procedure.Columns
            .GroupBy(column => column.TechnicalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Visible, StringComparer.OrdinalIgnoreCase);
        var metadata = result.Columns
            .GroupBy(column => column.TechnicalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var selected = result.Data.Columns
            .Cast<DataColumn>()
            .Select((column, index) => new { Column = column, Index = index })
            .Where(item => !visibility.TryGetValue(item.Column.ColumnName, out var visible) || visible)
            .ToArray();
        var columns = selected
            .Select(item => metadata.TryGetValue(item.Column.ColumnName, out var configured)
                ? configured
                : new GridColumnDto
                {
                    TechnicalName = item.Column.ColumnName,
                    Caption = item.Column.ColumnName,
                    Visible = true
                })
            .ToArray();
        var rows = result.Data.Rows
            .Cast<DataRow>()
            .Select(row => selected
                .Select(item => row[item.Index] is DBNull ? null : JsonSafe(row[item.Index]))
                .ToArray())
            .ToArray();

        return new
        {
            result.Success,
            result.ErrorMessage,
            result.ExecutionLogId,
            result.ProcedureId,
            result.ProcedureCaption,
            result.RowCount,
            result.SupportsPagination,
            result.PageNumber,
            result.PageSize,
            result.TotalRecords,
            columns,
            rows
        };
    }

    private static object? JsonSafe(object? value) => value switch
    {
        DBNull => null,
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime dateTime => dateTime,
        DateOnly date => date,
        TimeOnly time => time,
        TimeSpan timeSpan => timeSpan.ToString(),
        Guid guid => guid,
        bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or string
            or char => value,
        _ => value?.ToString()
    };
}
