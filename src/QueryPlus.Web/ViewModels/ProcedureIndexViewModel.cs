using Microsoft.AspNetCore.Mvc.Rendering;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Web.Models;

namespace QueryPlus.Web.ViewModels;

public sealed class ProcedureIndexViewModel
{
    public int? CategoryId { get; init; }
    public string? Caption { get; init; }
    public string? RoleEntitlement { get; init; }
    public bool? Enabled { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PagedResult<ProcedureListItemDto>.DefaultPageSize;
    public required PagedResult<ProcedureListItemDto> Result { get; init; }
    public IReadOnlyList<ProcedureListItemDto> Items => Result.Items;
    public required PagerModel Pager { get; init; }
    public required List<SelectListItem> CategoryOptions { get; init; }
}
