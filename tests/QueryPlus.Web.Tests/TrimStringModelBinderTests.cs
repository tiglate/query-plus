using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QueryPlus.Web.Infrastructure;

namespace QueryPlus.Web.Tests;

public class TrimStringModelBinderTests
{
    [Theory]
    [InlineData("  hello  ", "hello")]
    [InlineData("\talice\n", "alice")]
    [InlineData("no-trim", "no-trim")]
    [InlineData("   ", "")]
    public async Task BindModelAsync_TrimsWhitespace(string input, string expected)
    {
        var binder = new TrimStringModelBinder();
        var valueProvider = new SimpleValueProvider { ["Name"] = input };
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));
        var context = new DefaultModelBindingContext
        {
            ModelName = "Name",
            ModelMetadata = metadata,
            ModelState = new ModelStateDictionary(),
            ValueProvider = valueProvider,
            BindingSource = BindingSource.Form
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue();
        context.Result.Model.Should().Be(expected);
    }

    [Fact]
    public async Task BindModelAsync_LeavesMissingValueUnset()
    {
        var binder = new TrimStringModelBinder();
        var valueProvider = new SimpleValueProvider();
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));
        var context = new DefaultModelBindingContext
        {
            ModelName = "Missing",
            ModelMetadata = metadata,
            ModelState = new ModelStateDictionary(),
            ValueProvider = valueProvider,
            BindingSource = BindingSource.Form
        };

        await binder.BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
    }

    private sealed class SimpleValueProvider : Dictionary<string, string>, IValueProvider
    {
        public bool ContainsPrefix(string prefix) => Keys.Any(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        public ValueProviderResult GetValue(string key)
        {
            if (TryGetValue(key, out var value))
            {
                return new ValueProviderResult(value);
            }

            return ValueProviderResult.None;
        }
    }
}
