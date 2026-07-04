using Microsoft.OpenApi;

namespace ObsidianMCP.API;

internal static class SetupApi
{
    internal static void AddObsidianMcpApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ObsidianMCP API",
                Version = "v1",
                Description = """
                    REST API over an Obsidian/LeafWiki vault indexed with Lucene.NET. The same
                    functionality is also exposed as MCP tools at /mcp; this REST surface is
                    equivalent and useful for testing or for agents that consume OpenAPI directly.

                    Recommended call order:
                    1. POST /api/notes/index — reindex the vault (incremental, safe to call anytime;
                       returns 409 if one is already running). Also runs automatically in the
                       background, so this step can often be skipped.
                    2. GET /api/notes/search — find relevant notes by free-text search. Each result
                       includes a `path`.
                    3. GET /api/notes — fetch a note's full content, using the exact `path` value
                       returned by search.

                    Note content is always returned with YAML frontmatter already stripped.
                    """,
            });

            foreach (var xmlFile in new[] { "ObsidianMCP.API.xml", "ObsidianMCP.Application.xml" })
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }
        });
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
