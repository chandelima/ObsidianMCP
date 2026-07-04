using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ObsidianMCP.Infrastructure.Settings;

public sealed class ObsidianSettings
{
    public string VaultPath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;

    /// <summary>Intervalo (em minutos) entre reindexações automáticas. Se não configurado, usa 5.</summary>
    public int? ReindexIntervalMinutes { get; set; }

    public string IndexPath => Path.Combine(DataPath, "lucene-index");

    public string ManifestPath => Path.Combine(DataPath, "manifest.json");

    public static ObsidianSettings FromConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = configuration.GetSection("Obsidian").Get<ObsidianSettings>() ?? new ObsidianSettings();

        settings.VaultPath = ResolvePath(environment.ContentRootPath, settings.VaultPath);
        settings.DataPath = ResolvePath(environment.ContentRootPath, settings.DataPath);

        if (!Directory.Exists(settings.VaultPath))
        {
            throw new InvalidOperationException(
                $"Obsidian:VaultPath não está configurado ou não existe ('{settings.VaultPath}'). Defina um caminho válido em appsettings.json.");
        }

        return settings;
    }

    private static string ResolvePath(string contentRootPath, string path) =>
        string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
}
