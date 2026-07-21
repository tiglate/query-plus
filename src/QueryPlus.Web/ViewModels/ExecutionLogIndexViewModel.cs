using Microsoft.AspNetCore.Mvc.Rendering;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Web.Models;

namespace QueryPlus.Web.ViewModels;

public sealed class ExecutionLogIndexViewModel
{
    public string? Username { get; init; }
    public int? ProcedureId { get; init; }
    public bool? Success { get; init; }
    public DateTime? StartFrom { get; init; }
    public DateTime? StartTo { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PagedResult<ExecutionLogListItemDto>.DefaultPageSize;
    public required PagedResult<ExecutionLogListItemDto> Result { get; init; }
    public IReadOnlyList<ExecutionLogListItemDto> Items => Result.Items;
    public required PagerModel Pager { get; init; }
    public required List<SelectListItem> ProcedureOptions { get; init; }
}
