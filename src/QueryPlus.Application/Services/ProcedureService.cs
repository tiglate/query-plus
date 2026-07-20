using AutoMapper;
using FluentValidation;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class ProcedureService(
    IProcedureRepository procedures,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserContext currentUser,
    IValidator<SaveProcedureDto> saveValidator)
    : IProcedureService
{
    public async Task<PagedResult<ProcedureListItemDto>> SearchAsync(
        ProcedureFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = new ProcedureSearchCriteria
        {
            CategoryId = filter.CategoryId,
            Caption = filter.Caption,
            RoleEntitlement = filter.RoleEntitlement,
            Enabled = filter.Enabled,
            DatabaseName = filter.DatabaseName,
            ProcedureName = filter.ProcedureName
        };

        var (page, pageSize) = PagedResult<ProcedureListItemDto>.Normalize(filter.Page, filter.PageSize);

        var (items, totalCount) = await procedures.SearchAsync(criteria, page, pageSize, cancellationToken);

        if (totalCount > 0 && (page - 1) * pageSize >= totalCount)
        {
            (page, pageSize) = PagedResult<ProcedureListItemDto>.Normalize(page, pageSize, totalCount);
            (items, totalCount) = await procedures.SearchAsync(criteria, page, pageSize, cancellationToken);
        }

        return new PagedResult<ProcedureListItemDto>
        {
            Items = mapper.Map<IReadOnlyList<ProcedureListItemDto>>(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProcedureDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await procedures.GetByIdWithDetailsAsync(id, cancellationToken);
        return entity is null ? null : mapper.Map<ProcedureDetailDto>(entity);
    }

    public async Task<IReadOnlyList<ProcedureLookupDto>> GetAccessibleForCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = currentUser.Roles;
        var items = await procedures.GetAccessibleForExecutionAsync(roles, cancellationToken);
        return mapper.Map<IReadOnlyList<ProcedureLookupDto>>(items);
    }

    public async Task<ProcedureDetailDto> CreateAsync(
        SaveProcedureDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(saveValidator, dto, cancellationToken);
        await EnsureCategoryExistsAsync(dto.CategoryId, cancellationToken);
        await EnsureUniqueConstraintsAsync(dto, excludeId: null, cancellationToken);

        var entity = ProcedureGraphMapper.ToNewEntity(dto);
        await procedures.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await procedures.GetByIdWithDetailsAsync(entity.IdProcedure, cancellationToken)
            ?? entity;
        return mapper.Map<ProcedureDetailDto>(created);
    }

    public async Task<ProcedureDetailDto> UpdateAsync(
        SaveProcedureDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Id is null or <= 0)
        {
            throw new Common.ValidationException(nameof(dto.Id), "Procedure id is required for update.");
        }

        await ValidationHelper.ValidateAndThrowAsync(saveValidator, dto, cancellationToken);
        await EnsureCategoryExistsAsync(dto.CategoryId, cancellationToken);
        await EnsureUniqueConstraintsAsync(dto, dto.Id, cancellationToken);

        var entity = await procedures.GetByIdWithDetailsAsync(dto.Id.Value, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Procedure), dto.Id.Value);

        // Track removals for EF (collection remove alone may not mark deleted dependents
        // depending on cascade config — explicit remove for safety).
        var removedParameters = entity.Parameters
            .Where(p => dto.Parameters.All(d => d.Id != p.IdProcedureParameter))
            .ToList();
        var removedColumns = entity.Columns
            .Where(c => dto.Columns.All(d => d.Id != c.IdProcedureColumn))
            .ToList();

        ProcedureGraphMapper.ApplyUpdate(entity, dto);

        foreach (var p in removedParameters)
        {
            procedures.RemoveParameter(p);
        }

        foreach (var c in removedColumns)
        {
            procedures.RemoveColumn(c);
        }

        procedures.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await procedures.GetByIdWithDetailsAsync(entity.IdProcedure, cancellationToken)
            ?? entity;
        return mapper.Map<ProcedureDetailDto>(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await procedures.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Procedure), id);

        procedures.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new EntityNotFoundException(nameof(Category), categoryId);
        }
    }

    private async Task EnsureUniqueConstraintsAsync(
        SaveProcedureDto dto,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (await procedures.ExistsByCaptionAsync(dto.Caption.Trim(), excludeId, cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Caption), "A procedure with this caption already exists.");
        }

        if (await procedures.ExistsByDatabaseAndNameAsync(
                dto.DatabaseName.Trim(),
                dto.ProcedureName.Trim(),
                excludeId,
                cancellationToken))
        {
            throw new Common.ValidationException(
                nameof(dto.ProcedureName),
                "A procedure with this database and name already exists.");
        }
    }
}
