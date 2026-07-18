using AutoMapper;
using FluentValidation;
using QueryPlus.Application.Abstractions;
using QueryPlus.Application.DTOs.Procedures;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Mapping;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class ProcedureService : IProcedureService
{
    private readonly IProcedureRepository _procedures;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserContext _currentUser;
    private readonly IValidator<SaveProcedureDto> _saveValidator;

    public ProcedureService(
        IProcedureRepository procedures,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserContext currentUser,
        IValidator<SaveProcedureDto> saveValidator)
    {
        _procedures = procedures;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _saveValidator = saveValidator;
    }

    public async Task<IReadOnlyList<ProcedureListItemDto>> SearchAsync(
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

        var items = await _procedures.SearchAsync(criteria, cancellationToken);
        return _mapper.Map<IReadOnlyList<ProcedureListItemDto>>(items);
    }

    public async Task<ProcedureDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _procedures.GetByIdWithDetailsAsync(id, cancellationToken);
        return entity is null ? null : _mapper.Map<ProcedureDetailDto>(entity);
    }

    public async Task<IReadOnlyList<ProcedureLookupDto>> GetAccessibleForCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = _currentUser.Roles;
        var items = await _procedures.GetAccessibleForExecutionAsync(roles, cancellationToken);
        return _mapper.Map<IReadOnlyList<ProcedureLookupDto>>(items);
    }

    public async Task<ProcedureDetailDto> CreateAsync(
        SaveProcedureDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(_saveValidator, dto, cancellationToken);
        await EnsureCategoryExistsAsync(dto.CategoryId, cancellationToken);
        await EnsureUniqueConstraintsAsync(dto, excludeId: null, cancellationToken);

        var entity = ProcedureGraphMapper.ToNewEntity(dto);
        await _procedures.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _procedures.GetByIdWithDetailsAsync(entity.IdProcedure, cancellationToken)
            ?? entity;
        return _mapper.Map<ProcedureDetailDto>(created);
    }

    public async Task<ProcedureDetailDto> UpdateAsync(
        SaveProcedureDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Id is null or <= 0)
        {
            throw new Common.ValidationException(nameof(dto.Id), "Procedure id is required for update.");
        }

        await ValidationHelper.ValidateAndThrowAsync(_saveValidator, dto, cancellationToken);
        await EnsureCategoryExistsAsync(dto.CategoryId, cancellationToken);
        await EnsureUniqueConstraintsAsync(dto, dto.Id, cancellationToken);

        var entity = await _procedures.GetByIdWithDetailsAsync(dto.Id.Value, cancellationToken)
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
            _procedures.RemoveParameter(p);
        }

        foreach (var c in removedColumns)
        {
            _procedures.RemoveColumn(c);
        }

        _procedures.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _procedures.GetByIdWithDetailsAsync(entity.IdProcedure, cancellationToken)
            ?? entity;
        return _mapper.Map<ProcedureDetailDto>(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _procedures.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Procedure), id);

        _procedures.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(categoryId, cancellationToken);
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
        if (await _procedures.ExistsByCaptionAsync(dto.Caption.Trim(), excludeId, cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Caption), "A procedure with this caption already exists.");
        }

        if (await _procedures.ExistsByDatabaseAndNameAsync(
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
