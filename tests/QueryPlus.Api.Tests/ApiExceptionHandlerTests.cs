using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QueryPlus.Api.ProblemDetails;
using QueryPlus.Domain.Exceptions;

namespace QueryPlus.Api.Tests;

public class ApiExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetails = Substitute.For<IProblemDetailsService>();
    private readonly ApiExceptionHandler _sut;

    public ApiExceptionHandlerTests()
    {
        _sut = new ApiExceptionHandler(_problemDetails, NullLogger<ApiExceptionHandler>.Instance);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(403)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task TryHandleAsync_MapsExceptionToCorrectStatusCode(int expectedStatusCode)
    {
        var context = new DefaultHttpContext();
        Exception ex = expectedStatusCode switch
        {
            404 => new EntityNotFoundException("Category", 1),
            403 => new ForbiddenOperationException("Forbidden"),
            400 => new BusinessRuleException("Business rule error"),
            _ => new InvalidOperationException("Unexpected error")
        };

        _problemDetails.TryWriteAsync(Arg.Any<ProblemDetailsContext>()).Returns(ValueTask.FromResult(true));

        var handled = await _sut.TryHandleAsync(context, ex, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(expectedStatusCode);
    }
}
