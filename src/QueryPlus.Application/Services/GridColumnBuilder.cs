using System.Data;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Services;

public sealed class GridColumnBuilder : IGridColumnBuilder
{
    public IReadOnlyList<GridColumnDto> BuildGridColumns(Procedure procedure, DataTable data)
    {
        var configured = procedure.Columns
            .Where(c => c.Visible)
            .ToDictionary(c => c.TechnicalName, c => c, StringComparer.OrdinalIgnoreCase);

        var columns = new List<GridColumnDto>();
        foreach (DataColumn col in data.Columns)
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
                    Alignment = ColumnAlignment.Left,
                    Visible = true
                });
            }
        }

        return columns;
    }
}
