namespace QueryPlus.Web.Models;

/// <summary>
/// View model for the shared Excel-like sheet grid (Clusterize-backed).
/// </summary>
public sealed class SheetGridModel
{
    public IReadOnlyList<SheetGridColumn> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Cells { get; init; } = [];
    public string? EmptyMessage { get; init; }
    public string? MetaText { get; init; }
    public string? MetaHtml { get; init; }
    public bool ShowInteractionHint { get; init; } = true;
    /// <summary>Extra CSS classes on the root (e.g. qp-sheet-grid--admin).</summary>
    public string? CssClass { get; init; }
}

public sealed class SheetGridColumn
{
    public required string Caption { get; init; }
    public string Align { get; init; } = "left";
    public bool IsHtml { get; init; }
    public bool Sortable { get; init; } = true;
    /// <summary>Optional fixed pixel width (skips auto-size when set).</summary>
    public int? Width { get; init; }
}
