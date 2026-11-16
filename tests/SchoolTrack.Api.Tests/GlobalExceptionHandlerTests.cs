using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolTrack.Api.Middleware;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies status mapping and error-detail redaction in <see cref="GlobalExceptionHandler"/>.
/// </summary>
public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ArgumentException_ReturnsBadRequestProblemDetails()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new StubHostEnvironment("Production"));

        var context = CreateHttpContext("/api/v1/test");
        var handled = await handler.TryHandleAsync(
            context,
            new ArgumentException("Invalid bus code."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var problem = await ReadProblemDetailsAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Invalid bus code.", problem.Detail);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledExceptionInProduction_RedactsDetail()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new StubHostEnvironment("Production"));

        var context = CreateHttpContext("/api/v1/boom");
        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sensitive database password leak"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var problem = await ReadProblemDetailsAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.DoesNotContain("Sensitive database password leak", problem.Detail ?? string.Empty);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(problem);
        return problem!;
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "SchoolTrack.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

