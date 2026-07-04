using Microsoft.Extensions.DependencyInjection;
using ObsidianMCP.Application.Interfaces.Services;
using ObsidianMCP.Infrastructure.BackgroundServices;
using ObsidianMCP.Infrastructure.Services;
using ObsidianMCP.Infrastructure.Services.Lucene;
using ObsidianMCP.Infrastructure.Settings;

namespace ObsidianMCP.Infrastructure;

public static class SetupInfrastructure
{
    public static void AddObsidianMcpInfrastructure(this IServiceCollection services, ObsidianSettings settings)
    {
        Directory.CreateDirectory(settings.DataPath);

        services.AddSingleton<IFileManifestService, FileManifestService>();
        services.AddSingleton<IObsidianIndexService, LuceneObsidianIndexService>();
        services.AddHostedService<PeriodicReindexHostedService>();
    }
}
