namespace SchoolTrack.Api.Extensions;

/// <summary>
/// Configures OpenAPI specification generation and Swagger UI documentation.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Registers Swagger/OpenAPI generators for SchoolTrack API v1.
    /// </summary>
    public static IServiceCollection AddSchoolTrackSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "SchoolTrack Platform API",
                Version = "v1",
                Description = "Multi-tenant school transportation management, live GPS/RFID hardware ingestion, and safety notification API."
            });
        });

        return services;
    }

    /// <summary>
    /// Enables Swagger JSON and interactive UI middleware when configured.
    /// </summary>
    public static IApplicationBuilder UseSchoolTrackSwagger(
        this WebApplication app,
        IConfiguration configuration)
    {
        var enabled = app.Environment.IsDevelopment() || configuration.GetValue<bool>("Swagger:Enabled");
        if (!enabled)
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SchoolTrack API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
