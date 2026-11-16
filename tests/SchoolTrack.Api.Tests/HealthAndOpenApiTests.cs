using System.Net;
using System.Net.Http.Json;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies core Phase 1 HTTP pipeline endpoints: liveness, system metadata, and OpenAPI specification.
/// </summary>
public sealed class HealthAndOpenApiTests : IClassFixture<SchoolTrackWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="HealthAndOpenApiTests"/>.
    /// </summary>
    public HealthAndOpenApiTests(SchoolTrackWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsOkWithHealthyPayload()
    {
        var response = await _client.GetAsync(ApiRoutes.HealthLive);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SystemInfo_ReturnsOkWithApiVersionV1()
    {
        var response = await _client.GetAsync($"/{ApiRoutes.Version1Prefix}/system/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SystemInfoDto>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal("SchoolTrack.Api", payload.Data!.Service);
        Assert.Equal("v1", payload.Data.ApiVersion);
    }

    [Fact]
    public async Task SwaggerJson_ReturnsOkWithOpenApiDocument()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("SchoolTrack Platform API", json, StringComparison.Ordinal);
    }

    private sealed record SystemInfoDto(
        string Service,
        string ApiVersion,
        string Environment,
        DateTimeOffset UtcNow);
}

