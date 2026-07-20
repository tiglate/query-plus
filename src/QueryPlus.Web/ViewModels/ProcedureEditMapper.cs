using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;

namespace QueryPlus.Web.ViewModels;

public static class ProcedureEditMapper
{
    public static ProcedureEditViewModel FromDetail(ProcedureDetailDto dto, bool readOnly = false) => new()
    {
        Id = dto.Id,
        CategoryId = dto.CategoryId,
        Caption = dto.Caption,
        DatabaseName = dto.DatabaseName,
        ProcedureName = dto.ProcedureName,
        Enabled = dto.Enabled,
        SupportsPagination = dto.SupportsPagination,
        RoleEntitlement = dto.RoleEntitlement,
        Description = dto.Description,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        ReadOnly = readOnly,
        Parameters = dto.Parameters.Select(p => new ParameterEditViewModel
        {
            Id = p.Id,
            Caption = p.Caption,
            Name = p.Name,
            ParameterType = p.ParameterType,
            DefaultValue = p.DefaultValue,
            ComboValues = p.ComboValues,
            IsRequired = p.IsRequired
        }).ToList(),
        Columns = dto.Columns.Select(c => new ColumnEditViewModel
        {
            Id = c.Id,
            TechnicalName = c.TechnicalName,
            Caption = c.Caption,
            Alignment = c.Alignment,
            FormatMask = c.FormatMask,
            Visible = c.Visible
        }).ToList()
    };

    public static SaveProcedureDto ToSaveDto(ProcedureEditViewModel model) => new()
    {
        Id = model.Id,
        CategoryId = model.CategoryId,
        Caption = model.Caption,
        DatabaseName = model.DatabaseName,
        ProcedureName = model.ProcedureName,
        Enabled = model.Enabled,
        SupportsPagination = model.SupportsPagination,
        RoleEntitlement = model.RoleEntitlement,
        Description = model.Description,
        Parameters = model.Parameters.Select(p => new SaveProcedureParameterDto
        {
            Id = p.Id,
            Caption = p.Caption,
            Name = p.Name,
            ParameterType = p.ParameterType,
            DefaultValue = p.DefaultValue,
            ComboValues = p.ComboValues,
            IsRequired = p.IsRequired
        }).ToList(),
        Columns = model.Columns.Select(c => new SaveProcedureColumnDto
        {
            Id = c.Id,
            TechnicalName = c.TechnicalName,
            Caption = c.Caption,
            Alignment = c.Alignment,
            FormatMask = c.FormatMask,
            Visible = c.Visible
        }).ToList()
    };

    public static void ApplySnapshot(ProcedureEditViewModel model, ProcedureMetadataSnapshot snapshot)
    {
        model.Parameters = snapshot.Parameters.Select(p => new ParameterEditViewModel
        {
            Caption = p.Caption,
            Name = p.Name,
            ParameterType = p.ParameterType,
            DefaultValue = p.DefaultValue,
            ComboValues = p.ComboValues,
            IsRequired = p.IsRequired
        }).ToList();

        model.Columns = snapshot.Columns.Select(c => new ColumnEditViewModel
        {
            TechnicalName = c.TechnicalName,
            Caption = c.Caption,
            Alignment = c.Alignment,
            FormatMask = c.FormatMask,
            Visible = c.Visible
        }).ToList();
    }
}
