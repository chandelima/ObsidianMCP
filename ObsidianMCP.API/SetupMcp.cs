namespace ObsidianMCP.API;

internal static class SetupMcp
{
    internal static void AddObsidianMcpServer(this IServiceCollection services)
    {
        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();
    }

    internal static void MapObsidianMcpServer(this WebApplication app)
    {
        app.MapMcp("/mcp");
    }
}
