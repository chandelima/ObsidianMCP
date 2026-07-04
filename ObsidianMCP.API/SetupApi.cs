namespace ObsidianMCP.API;

internal static class SetupApi
{
    internal static void AddObsidianMcpApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Mcp-Session-Id"));
        });
    }

    internal static void ApplyObsidianMcpSettings(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        if (app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors();
        app.UseAuthorization();
        app.MapControllers();
    }
}
