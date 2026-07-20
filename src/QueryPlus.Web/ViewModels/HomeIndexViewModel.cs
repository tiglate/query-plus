using QueryPlus.Application.DTOs.Procedures;

namespace QueryPlus.Web.ViewModels;

public sealed class HomeIndexViewModel
{
    public int? ProcedureId { get; init; }
    public IReadOnlyList<ProcedureLookupDto> AccessibleProcedures { get; init; } = [];
    public ProcedureDetailDto? SelectedProcedure { get; init; }
}
