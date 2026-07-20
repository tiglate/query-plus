using AutoMapper;
using FluentValidation;
using QueryPlus.Application.DTOs.Categories;
using QueryPlus.Application.DTOs.Common;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Validation;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Exceptions;
using QueryPlus.Domain.Interfaces;

namespace QueryPlus.Application.Services;

public sealed class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IValidator<CreateCategoryDto> createValidator,
    IValidator<UpdateCategoryDto> updateValidator)
    : ICategoryService
{
    public async Task<PagedResult<CategoryListItemDto>> SearchAsync(
        CategoryFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagedResult<CategoryListItemDto>.Normalize(filter.Page, filter.PageSize);

        var (items, totalCount) = await categories.SearchAsync(
            filter.Description,
            page,
            pageSize,
            cancellationToken);

        // If the requested page is past the end (e.g. after deletes), clamp and re-fetch once.
        if (totalCount > 0 && (page - 1) * pageSize >= totalCount)
        {
            (page, pageSize) = PagedResult<CategoryListItemDto>.Normalize(page, pageSize, totalCount);
            (items, totalCount) = await categories.SearchAsync(
                filter.Description,
                page,
                pageSize,
                cancellationToken);
        }

        return new PagedResult<CategoryListItemDto>
        {
            Items = mapper.Map<IReadOnlyList<CategoryListItemDto>>(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await categories.GetAllAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<CategoryListItemDto>>(items);
    }

    public async Task<CategoryDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await categories.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : mapper.Map<CategoryDetailDto>(entity);
    }

    public async Task<CategoryDetailDto> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(createValidator, dto, cancellationToken);

        var description = dto.Description.Trim();
        if (await categories.ExistsByDescriptionAsync(description, cancellationToken: cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Description), "A category with this description already exists.");
        }

        var entity = new Category { Description = description };
        await categories.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<CategoryDetailDto>(entity);
    }

    public async Task<CategoryDetailDto> UpdateAsync(
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAndThrowAsync(updateValidator, dto, cancellationToken);

        var entity = await categories.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Category), dto.Id);

        var description = dto.Description.Trim();
        if (await categories.ExistsByDescriptionAsync(description, dto.Id, cancellationToken))
        {
            throw new Common.ValidationException(nameof(dto.Description), "A category with this description already exists.");
        }

        entity.Description = description;
        categories.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<CategoryDetailDto>(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await categories.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Category), id);

        if (await categories.HasProceduresAsync(id, cancellationToken))
        {
            throw new BusinessRuleException("Cannot delete a category that still has procedures.");
        }

        categories.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
