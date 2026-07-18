using FluentValidation;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Application.Validation;

public sealed class SaveProcedureDtoValidator : AbstractValidator<SaveProcedureDto>
{
    public SaveProcedureDtoValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Caption).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DatabaseName)
            .NotEmpty()
            .MaximumLength(128)
            .Must(SqlIdentifier.IsValidSegment)
            .WithMessage("Database name must be a valid SQL identifier.");
        RuleFor(x => x.ProcedureName)
            .NotEmpty()
            .MaximumLength(128)
            .Must(SqlIdentifier.IsValidProcedureName)
            .WithMessage("Procedure name must be a valid SQL identifier (optionally schema-qualified).");
        RuleFor(x => x.RoleEntitlement).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);

        RuleForEach(x => x.Parameters).SetValidator(new SaveProcedureParameterDtoValidator());
        RuleForEach(x => x.Columns).SetValidator(new SaveProcedureColumnDtoValidator());

        RuleFor(x => x.Parameters)
            .Must(p => p.Select(x => NormalizeName(x.Name)).Distinct(StringComparer.OrdinalIgnoreCase).Count() == p.Count)
            .WithMessage("Parameter names must be unique within the procedure.");

        RuleFor(x => x.Columns)
            .Must(c => c.Select(x => x.TechnicalName.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == c.Count)
            .WithMessage("Column technical names must be unique within the procedure.");
    }

    private static string NormalizeName(string name)
    {
        var t = name.Trim();
        return t.StartsWith('@') ? t : $"@{t}";
    }
}

public sealed class SaveProcedureParameterDtoValidator : AbstractValidator<SaveProcedureParameterDto>
{
    public SaveProcedureParameterDtoValidator()
    {
        RuleFor(x => x.Caption).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128)
            .Must(n =>
            {
                var name = n.Trim().TrimStart('@');
                return SqlIdentifier.IsValidSegment(name);
            })
            .WithMessage("Parameter name must be a valid SQL identifier (optional leading @).");
        RuleFor(x => x.ParameterType).IsInEnum();
        RuleFor(x => x.DefaultValue).MaximumLength(500);
        RuleFor(x => x.ComboValues)
            .Must(JsonHelpers.IsValidStringArrayJson)
            .WithMessage("Combo values must be a valid JSON string array.")
            .When(x => !string.IsNullOrWhiteSpace(x.ComboValues));
        RuleFor(x => x.ComboValues)
            .NotEmpty()
            .When(x => x.ParameterType == ParameterType.Combo)
            .WithMessage("Combo parameters require ComboValues JSON array.");
    }
}

public sealed class SaveProcedureColumnDtoValidator : AbstractValidator<SaveProcedureColumnDto>
{
    public SaveProcedureColumnDtoValidator()
    {
        RuleFor(x => x.TechnicalName)
            .NotEmpty()
            .MaximumLength(128)
            .Must(SqlIdentifier.IsValidSegment)
            .WithMessage("Technical name must be a valid SQL identifier.");
        RuleFor(x => x.Caption).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Alignment).IsInEnum();
        RuleFor(x => x.FormatMask).MaximumLength(100);
    }
}
