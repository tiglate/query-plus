using System.Data;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Services;

public interface IGridColumnBuilder
{
    IReadOnlyList<GridColumnDto> BuildGridColumns(Procedure procedure, DataTable data);
}
