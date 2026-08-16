using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Warehouse.Api.Middleware;
using Warehouse.Application.Common;

namespace Warehouse.Api.Tests;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _sut = new(NullLogger<GlobalExceptionHandler>.Instance);

    private static async Task<(int StatusCode, string Body)> InvokeAsync(GlobalExceptionHandler handler, Exception exception)
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        handled.Should().BeTrue("the handler is expected to handle every exception type it's given, not fall through");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task TryHandleAsync_ConcurrencyConflictException_Returns409WithFriendlyMessage()
    {
        var (statusCode, body) = await InvokeAsync(_sut, new ConcurrencyConflictException("inner detail", new Exception()));

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        body.Should().Contain("just changed by someone else");
        body.Should().NotContain("inner detail");
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_Returns500WithoutLeakingExceptionDetail()
    {
        var (statusCode, body) = await InvokeAsync(_sut, new InvalidOperationException("super secret stack detail"));

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Should().Contain("unexpected error");
        body.Should().NotContain("super secret stack detail");
    }

    [Fact]
    public async Task TryHandleAsync_ResponseBody_IsAPlainJsonStringNotAnObject()
    {
        // Matches the shape ResultExtensions.ToActionResult() already produces for
        // expected failures (a raw string body), so the frontend's
        // `alert(error.response?.data || fallback)` pattern works the same way
        // whether the failure was anticipated or not.
        var (_, body) = await InvokeAsync(_sut, new InvalidOperationException("boom"));

        body.Should().StartWith("\"").And.EndWith("\"");
    }
}
