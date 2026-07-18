using AutoMapper;
using FluentValidation;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;

    public CategoryService(
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> SearchAsync(
        CategoryFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _categories.SearchAsync(filter.Description, cancellationToken);
        return _mapper.Map<IReadOnlyList<CategoryListItemDto>>(items);
    }

    public async Task<CategoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : _mapper.Map<CategoryDetailDto>(entity);
    }

    public async Task<CategoryDetailDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(_createValidator, dto, cancellationToken);

        var description = dto.Description.Trim();
        if (await _categories.ExistsByDescriptionAsync(description, cancellationToken: cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Description), "A category with this description already exists.");
        }

        var entity = new Category { Description = description };
        await _categories.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDetailDto>(entity);
    }

    public async Task<CategoryDetailDto> UpdateAsync(
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(_updateValidator, dto, cancellationToken);

        var entity = await _categories.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Category), dto.Id);

        var description = dto.Description.Trim();
        if (await _categories.ExistsByDescriptionAsync(description, dto.Id, cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Description), "A category with this description already exists.");
        }

        entity.Description = description;
        _categories.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDetailDto>(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Category), id);

        if (await _categories.HasProceduresAsync(id, cancellationToken))
        {
            throw new BusinessRuleException("Cannot delete a category that still has procedures.");
        }

        _categories.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
