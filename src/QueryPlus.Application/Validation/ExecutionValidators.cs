using FluentValidation;
using QueryPlus.Application.DTOs.Execution;

namespace QueryPlus.Application.Validation;

public sealed class ExecuteProcedureRequestValidator : AbstractValidator<ExecuteProcedureRequest>
{
    public ExecuteProcedureRequestValidator()
    {
        RuleFor(x => x.ProcedureId).GreaterThan(0);
        RuleFor(x => x.ParameterValues).NotNull();
    }
}
