using System.ComponentModel.DataAnnotations;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Web.ViewModels;

public sealed class ProcedureEditViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Category")]
    [Range(1, int.MaxValue, ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Display(Name = "Caption")]
    [Required(ErrorMessage = "Caption is required.")]
    [StringLength(300, ErrorMessage = "Caption must be at most 300 characters.")]
    public string Caption { get; set; } = string.Empty;

    [Display(Name = "Database")]
    [Required(ErrorMessage = "Database name is required.")]
    [StringLength(128, ErrorMessage = "Database name must be at most 128 characters.")]
    public string DatabaseName { get; set; } = string.Empty;

    [Display(Name = "Procedure name")]
    [Required(ErrorMessage = "Procedure name is required.")]
    [StringLength(128, ErrorMessage = "Procedure name must be at most 128 characters.")]
    public string ProcedureName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When true, the home grid uses server-side pagination (@PageNumber/@PageSize/@TotalRecords).
    /// </summary>
    [Display(Name = "Supports pagination")]
    public bool SupportsPagination { get; set; }

    [Display(Name = "Role / Entitlement")]
    [Required(ErrorMessage = "Role / Entitlement is required.")]
    [StringLength(100, ErrorMessage = "Role / Entitlement must be at most 100 characters.")]
    public string RoleEntitlement { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public List<ParameterEditViewModel> Parameters { get; set; } = [];
    public List<ColumnEditViewModel> Columns { get; set; } = [];

    public bool ReadOnly { get; set; }
}

public sealed class ParameterEditViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Parameter caption is required.")]
    [StringLength(200)]
    public string Caption { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parameter name is required.")]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public ParameterType ParameterType { get; set; } = ParameterType.FreeText;

    [StringLength(500)]
    public string? DefaultValue { get; set; }

    public string? ComboValues { get; set; }

    public bool IsRequired { get; set; }
}

public sealed class ColumnEditViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Technical name is required.")]
    [StringLength(128)]
    public string TechnicalName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Column caption is required.")]
    [StringLength(200)]
    public string Caption { get; set; } = string.Empty;

    public ColumnAlignment Alignment { get; set; } = ColumnAlignment.Left;

    [StringLength(100)]
    public string? FormatMask { get; set; }

    public bool Visible { get; set; } = true;
}
