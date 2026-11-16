using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Test server factory configuring <see cref="Program"/> for isolated ASP.NET Core integration checks.
/// </summary>
public sealed class SchoolTrackWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:Enabled"] = "true"
            });
        });
    }
}

