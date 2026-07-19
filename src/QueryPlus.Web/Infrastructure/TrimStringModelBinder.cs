using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace QueryPlus.Web.Infrastructure;

/// <summary>
/// Trims leading/trailing whitespace from all string values bound from forms, query, and routes.
/// </summary>
public sealed class TrimStringModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (value is null)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(value.Trim());
        return Task.CompletedTask;
    }
}

/// <summary>
/// Registers <see cref="TrimStringModelBinder"/> for every <see cref="string"/> model.
/// Insert at the start of <see cref="Microsoft.AspNetCore.Mvc.MvcOptions.ModelBinderProviders"/>.
/// </summary>
public sealed class TrimStringModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(string))
        {
            return new TrimStringModelBinder();
        }

        return null;
    }
}
