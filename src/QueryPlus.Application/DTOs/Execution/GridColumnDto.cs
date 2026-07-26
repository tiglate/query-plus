using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.DTOs.Execution;

public sealed class GridColumnDto
{
    public required string TechnicalName { get; init; }
    public required string Caption { get; init; }
    public ColumnAlignment Alignment { get; init; }
    public string? FormatMask { get; init; }
    public bool Visible { get; init; } = true;
}
