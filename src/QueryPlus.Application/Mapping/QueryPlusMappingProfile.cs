using AutoMapper;
using QueryPlus.Application.Common;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Execution;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Application.Mapping;

public sealed class QueryPlusMappingProfile : Profile
{
    public QueryPlusMappingProfile()
    {
        CreateMap<Category, CategoryListItemDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdCategory));

        CreateMap<Category, CategoryDetailDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdCategory))
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore());

        CreateMap<Procedure, ProcedureListItemDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdProcedure))
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.IdCategory))
            .ForMember(d => d.CategoryDescription, o => o.MapFrom(s => s.Category != null ? s.Category.Description : null));

        CreateMap<Procedure, ProcedureLookupDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdProcedure))
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.IdCategory))
            .ForMember(d => d.CategoryDescription, o => o.MapFrom(s => s.Category != null ? s.Category.Description : null));

        CreateMap<Procedure, ProcedureDetailDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdProcedure))
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.IdCategory))
            .ForMember(d => d.CategoryDescription, o => o.MapFrom(s => s.Category != null ? s.Category.Description : null))
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            // Never expose reserved pagination parameters on the detail/parameter form.
            .ForMember(d => d.Parameters, o => o.MapFrom(s =>
                s.Parameters
                    .Where(p => !ProcedurePagination.IsReservedParameterName(p.Name))
                    .OrderBy(p => p.Caption)))
            .ForMember(d => d.Columns, o => o.MapFrom(s => s.Columns.OrderBy(c => c.Caption)));

        CreateMap<ProcedureParameter, ProcedureParameterDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdProcedureParameter))
            .ForMember(d => d.ComboOptions, o => o.MapFrom(s => JsonHelpers.ParseStringArray(s.ComboValues)));

        CreateMap<ProcedureColumn, ProcedureColumnDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdProcedureColumn));

        CreateMap<ProcedureColumn, GridColumnDto>();

        CreateMap<ExecutionLog, ExecutionLogDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdExecutionLog))
            .ForMember(d => d.ProcedureId, o => o.MapFrom(s => s.IdProcedure))
            .ForMember(d => d.ParameterValuesJson, o => o.MapFrom(s => s.ParameterValues));

        CreateMap<ExecutionLog, ExecutionLogListItemDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.IdExecutionLog))
            .ForMember(d => d.ProcedureId, o => o.MapFrom(s => s.IdProcedure))
            .ForMember(d => d.ProcedureCaption, o => o.MapFrom(s => s.Procedure.Caption));
    }
}
